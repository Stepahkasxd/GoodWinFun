# AGENTS.md — План реализации чтения/редактирования `dotakeys_personal.lst` в WPF (.NET 8, MVVM)

## Цели
- Найти `dotakeys_personal.lst` в Steam userdata.
- Прочитать Valve KeyValues (`"KeyBindings" → "Keys"`), распарсить все бинды.
- Сгруппировать по понятным категориям (скиллы, предметы, интерфейс и т.д.).
- Показать дружелюбные названия в UI (WPF + CommunityToolkit.Mvvm + Extended.Wpf.Toolkit).
- Разрешить редактирование клавиш и безопасную запись обратно в `.lst` (с бэкапом и проверками).
- Слежение за внешними изменениями файла (Steam Cloud / сама Dota).

---

## 1) Поиск файла в Steam userdata

**Пути:**
- Windows по умолчанию: `C:\Program Files (x86)\Steam\userdata\*\570\remote\cfg\dotakeys_personal.lst`
- Steam может быть установлен не по умолчанию → прочитать `HKEY_CURRENT_USER\Software\Valve\Steam\SteamPath`
- В userdata может быть несколько SteamID → выбрать активный (последнее изменение файла) или дать выбор в UI.

**Сервис:**

```csharp
public interface ISteamPathService
{
    string? GetSteamRoot();
    IEnumerable<string> EnumerateDotaKeyFiles();
    string? SuggestMostRecentDotakeys();
}

public sealed class SteamPathService : ISteamPathService
{
    public string? GetSteamRoot()
    {
        using var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        var raw = rk?.GetValue("SteamPath") as string;
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw.Replace('/', '\\');
    }

    public IEnumerable<string> EnumerateDotaKeyFiles()
    {
        var root = GetSteamRoot();
        if (root is null) yield break;
        var glob = System.IO.Path.Combine(root, "userdata");
        if (!System.IO.Directory.Exists(glob)) yield break;
        foreach (var id in System.IO.Directory.EnumerateDirectories(glob))
        {
            var path = System.IO.Path.Combine(id, @"570\remote\cfg\dotakeys_personal.lst");
            if (System.IO.File.Exists(path)) yield return path;
        }
    }

    public string? SuggestMostRecentDotakeys()
        => EnumerateDotaKeyFiles()
           .OrderByDescending(p => System.IO.File.GetLastWriteTimeUtc(p))
           .FirstOrDefault();
}
```

---

## 2) Парсер Valve KeyValues (без внешних зависимостей)

Файл — «KV1»-подобный формат: кавычки, пары ключ-значение и вложенные блоки `{}`.
Нужны только два уровня: `"KeyBindings"` → `"Keys"` → блоки действий.

**Модель данных:**

```csharp
public sealed class KeybindEntry
{
    public string Label { get; set; } = "";      // верхний ключ блока, напр. "Ability1"
    public string Name { get; set; } = "";       // "Name"
    public string Action { get; set; } = "";     // "Action"
    public string Key { get; set; } = "";        // "Key" (может быть пустой)
    public string Panel { get; set; } = "";      // "#DOTA_KEYBIND_MENU_..."
    public string PanelRow { get; set; } = "";
    public string Description { get; set; } = ""; // "#DOTA_..."
    public string Version { get; set; } = "";
}
```

**Парсер (минимальный, устойчивый к лишним полям):**

```csharp
public static class DotaKeyvalues
{
    private static readonly Regex BlockRegex = new(@"\s*""([^""]+)""\s*\{(.*?)\}", RegexOptions.Singleline);
    private static readonly Regex PairRegex  = new(@"\s*""([^""]+)""\s*""([^""]*)""", RegexOptions.Singleline);

    public static IReadOnlyList<KeybindEntry> Parse(string text)
    {
        // Вырезаем блок "Keys"
        var keysMatch = Regex.Match(text, @"""Keys""\s*\{(.*)\}\s*$", RegexOptions.Singleline | RegexOptions.Multiline);
        if (!keysMatch.Success) return Array.Empty<KeybindEntry>();
        var keysBody = keysMatch.Groups[1].Value;

        var result = new List<KeybindEntry>();
        foreach (Match m in BlockRegex.Matches(keysBody))
        {
            var label = m.Groups[1].Value;
            var body  = m.Groups[2].Value;
            var dict  = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match p in PairRegex.Matches(body))
                dict[p.Groups[1].Value] = p.Groups[2].Value;

            result.Add(new KeybindEntry {
                Label = label,
                Name = dict.GetValueOrDefault("Name",""),
                Action = dict.GetValueOrDefault("Action",""),
                Key = dict.GetValueOrDefault("Key",""),
                Panel = dict.GetValueOrDefault("Panel",""),
                PanelRow = dict.GetValueOrDefault("PanelRow",""),
                Description = dict.GetValueOrDefault("Description",""),
                Version = dict.GetValueOrDefault("Version",""),
            });
        }
        return result;
    }

    public static string Serialize(IEnumerable<KeybindEntry> entries, string originalText)
    {
        // Простая перезапись "Key" в исходном тексте.
        // Для сохранения порядка и неизвестных полей: заменяем внутри каждого блока по Label.
        string result = originalText;
        foreach (var e in entries)
        {
            // Замена строки "Key" "..." внутри соответствующего блока:
            var pattern = $"("{Regex.Escape(e.Label)}"\s*\{{)(.*?)(\}})";
            result = Regex.Replace(result, pattern, m => ReplaceKeyInBlock(m, e.Key), RegexOptions.Singleline);
        }
        return result;

        static string ReplaceKeyInBlock(Match block, string newKey)
        {
            var header = block.Groups[1].Value;
            var body   = block.Groups[2].Value;
            var tail   = block.Groups[3].Value;
            var keyLine = new System.Text.RegularExpressions.Regex("(\n\s*"Key"\s*")([^"]*)("")?");
            if (Regex.IsMatch(body, "\n\s*"Key"\s*"[^"]*""))
                body = Regex.Replace(body, "(\n\s*"Key"\s*")([^"]*)(")", $"\g<1>{newKey}\g<3>");
            else
                body += $"\n\t\t\t"Key"\t\t"{newKey}"";
            return header + body + tail;
        }
    }
}
```

> Можно заменить на полноценный KV-парсер (ValveKeyValueParser), но выше хватает для задачи и не тащит внешние пакеты.

---

## 3) Категоризация и дружелюбные названия

Удобная евристика — использовать поле `Panel` (оно уже делит на группы) и частично `Description`.

**Маппинг панелей → категории (RU):**

```csharp
public static class DotaCategories
{
    public static readonly IReadOnlyDictionary<string,string> PanelToCategory = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
    {
        ["#DOTA_KEYBIND_MENU_ABILITIES"] = "Кнопки для скиллов",
        ["#DOTA_KEYBIND_MENU_ITEMS"]     = "Кнопки предметов",
        ["#DOTA_KEYBIND_MENU_UNIT"]      = "Управление юнитами",
        ["#DOTA_KEYBIND_MENU_INTERFACE"] = "Кнопки интерфейса",
        ["#DOTA_KEYBIND_MENU_CHAT"]      = "Чат и коммуникация",
        ["#DOTA_KEYBIND_MENU_PLAYER"]    = "Игровые действия",
        ["#DOTA_KEYBIND_MENU_CAMERA"]    = "Камера",
        ["#DOTA_KEYBIND_MENU_SHOP"]      = "Магазин",
        ["#DOTA_KEYBIND_MENU_CONTROLGROUPS"] = "Группы управления",
        ["#DOTA_KEYBIND_MENU_SPECTATOR"] = "Зрительский режим",
        // fallback
    };

    public static string ToCategory(string panel)
        => PanelToCategory.TryGetValue(panel ?? "", out var v) ? v : "Прочее";
}
```

**Маппинг описаний → дружелюбный текст:**

```csharp
public static class DotaFriendly
{
    public static readonly Dictionary<string,string> DescriptionToRu = new(StringComparer.OrdinalIgnoreCase)
    {
        ["#DOTA_Ability1"] = "Первый скилл",
        ["#DOTA_Ability2"] = "Второй скилл",
        ["#DOTA_Ability3"] = "Третий скилл",
        ["#DOTA_AbilityUltimate"] = "Ультимейт",
        ["#DOTA_ItemSlot1"] = "Предмет 1",
        ["#DOTA_ItemSlot2"] = "Предмет 2",
        ["#DOTA_ItemSlot3"] = "Предмет 3",
        ["#DOTA_ItemSlot4"] = "Предмет 4",
        ["#DOTA_ItemSlot5"] = "Предмет 5",
        ["#DOTA_ItemSlot6"] = "Предмет 6",
        ["#DOTA_ActivateGlyph"] = "Глиф укрепления",
        ["#DOTA_ToggleConsole"] = "Консоль",
        ["#DOTA_ChatTeam"] = "Открыть чат (команда)",
        ["#DOTA_ChatAll"] = "Общий чат",
        ["#DOTA_CourierDeliver"] = "Курьер: принести",
    };

    public static string MakeFriendly(KeybindEntry e)
    {
        if (DescriptionToRu.TryGetValue(e.Description, out var ru)) return ru;
        if (!string.IsNullOrWhiteSpace(e.Name)) return SplitCamelCase(e.Name);
        if (!string.IsNullOrWhiteSpace(e.Action)) return e.Action.Replace("_", " ");
        return e.Label;
    }

    private static string SplitCamelCase(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s, "([a-z])([A-Z])", "$1 $2").Trim();
}
```

---

## 4) MVVM-структура

**ViewModels:**
- `MainViewModel` — выбор профиля Steam / путь к файлу, список категорий, команды Save/Reload.
- `CategoryViewModel` — `Name` + коллекция `KeybindItemViewModel`.
- `KeybindItemViewModel` — обёртка над `KeybindEntry` с полями: `FriendlyName`, `Key`, `Action`, команды смены клавиши.

```csharp
public partial class KeybindItemViewModel : ObservableObject
{
    public KeybindEntry Model { get; }
    public string Category { get; }
    public string FriendlyName => DotaFriendly.MakeFriendly(Model);

    [ObservableProperty]
    private string key;

    public KeybindItemViewModel(KeybindEntry m)
    {
        Model = m;
        Category = DotaCategories.ToCategory(m.Panel);
        key = m.Key ?? "";
    }
}
```

**Группировка для UI:**
```csharp
var entries = DotaKeyvalues.Parse(File.ReadAllText(path));
var items = entries.Select(e => new KeybindItemViewModel(e)).ToList();
var groups = items.GroupBy(i => i.Category)
                  .OrderBy(g => g.Key)
                  .Select(g => new CategoryViewModel(g.Key, g.ToList()));
```

**XAML (набросок):**
- Слева `ListBox` категорий, справа `DataGrid` биндов категории.
- Для ввода клавиш использовать обычный `TextBox` (с авто-комплитом whitelist), либо кастомный capture-контрол.
- Храним строковые значения как в `.lst`: `Q`, `W`, `RETURN`, `OEM3`, `UPARROW` и т.д.

---

## 5) Валидация и конфликты

- Дубликаты: если одна клавиша назначена на несколько действий — подсветка (ValidationRule).
- Whitelist специальных имён: `SPACE`, `RETURN`, `ESCAPE`, `TAB`, `BACKSPACE`, `OEM3`, `UPARROW`, `DOWNARROW`, `LEFTARROW`, `RIGHTARROW`, `MOUSE1`, `MOUSE2`, `MWHEELUP`, `MWHEELDOWN` и т.п.
- Модификаторы типа Ctrl/Alt/Shift обычно не пишутся как `Ctrl+Q` — использовать нативные имена из файла.

---

## 6) Сохранение

- Перед записью сделать бэкап `dotakeys_personal.lst.bak`.
- Обновить `Model.Key` из VM → `Serialize(entries, originalText)` → запись.
- `FileSystemWatcher` следит за внешними изменениями.

```csharp
public async Task SaveAsync()
{
    var text = await File.ReadAllTextAsync(CurrentPath);
    var entries = CurrentItems.Select(i => { i.Model.Key = i.Key; return i.Model; }).ToList();

    var backup = CurrentPath + ".bak";
    File.Copy(CurrentPath, backup, overwrite:true);

    var newText = DotaKeyvalues.Serialize(entries, text);
    await File.WriteAllTextAsync(CurrentPath, newText);
}
```

---

## 7) UX: «человеческие» подписи

Колонки: **Действие**, **Клавиша**, *(серым)* **Команда Dota**. Примеры:
- Первый скилл → Q
- Второй скилл → W
- Третий скилл → E
- Ультимейт → R
- Предмет 1 → Z … Предмет 6 → N
- Консоль → Oem3
- Открыть чат (команда) → Return
- Общий чат → Y

---

## 8) Краевые случаи
- Нет файла — предложить выбор профиля или «Обновить / Создать».
- Steam Cloud переписывает → после сохранения дождаться обновления таймстемпа и перечитать при расхождении.
- Локализация ключей пополняется по телеметрии (неизвестные ключи добавлять в словарь).

---

## 9) Расширения
- Пресеты раскладок (QWER/ASDF/ESDF).
- Экспорт/импорт профилей в JSON.
- Подсветка конфликтов в пределах и между категориями.

---

## 10) Check-list
1. `SteamPathService` — поиск и выбор пути.
2. Парсер `DotaKeyvalues.Parse`.
3. Категоризация `DotaCategories` + дружественные названия `DotaFriendly`.
4. MVVM-обёртки и XAML.
5. Валидация и whitelist.
6. Сохранение с бэкапом `Serialize` + `FileSystemWatcher`.
