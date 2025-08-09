using System.Collections.Generic;

namespace GoodWin.Keybinds;

public static class DotaCategories
{
    private static readonly Dictionary<string, string> PanelToCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["#DOTA_KEYBIND_MENU_ABILITIES"] = "Кнопки для скиллов",
        ["#DOTA_KEYBIND_MENU_ITEMS"] = "Кнопки предметов",
        ["#DOTA_KEYBIND_MENU_UNIT"] = "Управление юнитами",
        ["#DOTA_KEYBIND_MENU_INTERFACE"] = "Кнопки интерфейса",
        ["#DOTA_KEYBIND_MENU_CHAT"] = "Чат и коммуникация",
        ["#DOTA_KEYBIND_MENU_PLAYER"] = "Игровые действия",
        ["#DOTA_KEYBIND_MENU_CAMERA"] = "Камера",
        ["#DOTA_KEYBIND_MENU_SHOP"] = "Магазин",
        ["#DOTA_KEYBIND_MENU_CONTROLGROUPS"] = "Группы управления",
        ["#DOTA_KEYBIND_MENU_SPECTATOR"] = "Зрительский режим",
    };

    public static string ToCategory(string panel)
        => PanelToCategory.TryGetValue(panel, out var c) ? c : "Прочее";
}

