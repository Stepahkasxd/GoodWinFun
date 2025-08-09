# AGENT.md — План доработки проблемных дебафов (WPF, .NET 8, MVVM)

> **Важно:** Консольные дебафы, которые уже стабильно работают через `JoyCommandService` (изменение cvars, camera flags и т.п.), здесь не описываются. В документ включены только те, где текущая реализация расходится с задумкой или может быть существенно улучшена.

---

## Соглашения и окружение

- Стек: **.NET 8**, **WPF**, **CommunityToolkit.Mvvm**, **Extended.Wpf.Toolkit**.
- Базовые утилиты проекта: `InputHookHost` (LL-hooks/SendInput), `OverlayWindow` (прозрачный оверлей), `WindowHelper`.
- Требование для визуальных оверлеев: **Borderless/Windowed** режим в Dota 2 (в exclusive fullscreen оверлеи не видны).

---

## Таблица статуса «хотелки → факт»

| Дебаф | Что задумано | Что сейчас | Статус | Суть доработки |
|---|---|---|---|---|
| RainbowDebuff | RGB‑эффект «у всего» (игра, курсор, интерфейс), real‑time | Полупрозрачный WPF‑оверлей с радужным градиентом | ❌ | Захват кадра + GPU Pixel Shader (Hue Shift) + полноэкранный вывод |
| MiniGameDebuff | Мини‑игра «починить проводку» (как в Among Us) + блок ввода | Простейшая заглушка (клики) | ❌ | WPF‑Canvas, порты слева/справа, Bezier‑провода, snap, Completed |
| PressAllItemsDebuff | Жать реальные слоты предметов из `dotakeys_personal.lst` | Жёстко `Z X C V B N` | ❌ | Парсить `.lst` → Inventory1..6 → жать реальные клавиши |
| PressAllSkillsDebuff | Жать реальные Ability1..6/Ultimate из `.lst` | Жёстко `Q W E R D F` | ❌ | Парсить `.lst` → Ability* → жать реальные клавиши |
| BuyTeleportsDebuff | Купить 5 TP «на любом разрешении» + доставить курьером | Клик по фикс. координатам | ❌ | Поиск кнопок по картинке (OpenCV) внутри окна Dota |
| InputLagDebuff / MouseLag | Плавная задержка курсора 0.33s (без рывков) | Задержка есть, но без плавного playback | ⚠ | Буфер событий + интерполяция и воспроизведение по таймеру |

---

## 1) RainbowDebuff — полноэкранный RGB‑эффект

**Задумка.** Цветовой «перелив» *у всего*, включая Dota, курсор и другие окна, в реальном времени.

**Проблема сейчас.** WPF‑оверлей не перекрашивает сам рендер окна/курсора — это плёнка. Нужен пост‑процессинг кадра (GPU).

**Решение.**
1. Захват кадра экрана/окна Dota через **DXGI** (Desktop Duplication) или **GraphicsCapturePicker** (Win10+; но для WPF обычно — DXGI).
2. Применение **Pixel Shader** (HLSL) с Hue Shift (циклический сдвиг тона).
3. Отрисовка в полноэкранный бэкбуфер (SwapChain) поверх экрана (окно с `WS_EX_LAYERED` и мышепроницаемостью).

**Библиотеки.** `Vortice.Windows` (современная обёртка DirectX), либо `SharpDX` (устар.), либо WPF `D3DImage` + ShaderEffect (если достаточно оверлея над снимком окна).
  
**Скелет (псевдо):**
```csharp
// Setup DX device + swapchain (Vortice.Direct3D11)
// Захват D3D11 texture (Desktop Duplication)
// Pixel shader: HueShift.cso (компилируется из HLSL)
while (running)
{
    var frame = desktopDuplication.GetFrame();      // ID3D11Texture2D
    context.PixelShader.Set(hueShiftPs);
    context.PSSetShaderResources(0, frameSrv);
    context.DrawFullScreenQuad();
    swapChain.Present(1, PresentFlags.None);
}
```

**HLSL (идея):**
```hlsl
float hueShift; // 0..1, плавная анимация
float4 main(float2 uv : TEXCOORD) : SV_Target
{
    float3 rgb = tex.Sample(samp, uv).rgb;
    float3 hsv = RgbToHsv(rgb);
    hsv.x = frac(hsv.x + hueShift);
    return float4(HsvToRgb(hsv), 1);
}
```

---

## 2) MiniGameDebuff — «Починить проводку» (Among Us)

**Задумка.** Всплывает мини‑игра *как в оригинале*: 4–6 портов слева/справа (цвет/иконка), тянем «провода» от левых портов к соответствующим правым. Пока не решишь — блок ввода.

**Текущая реализация.** Заглушка, без визуала проводки и проверки совпадений.

**Решение.**
- Окно‑оверлей поверх Dota по хэндлу: `FindWindow("Dota 2")` → `GetWindowRect` → DPI‑aware позиционирование.
- Вёрстка: `Canvas`. Порты — `Ellipse`/`Button` с `Tag=Id/Color`. Провода — `Path` с `PathGeometry` (Bezier).
- Drag‑логика: `MouseDown` на левом порту → создаём `WireVM` и ведём конец за курсором → `MouseUp` на правом порту — проверяем `Id`.
- Блокировка ввода: `InputHookHost.BlockAllKeys/Mouse`, **кроме** событий внутри оверлея (или whitelist паник‑горячей).
- Сигнал завершения: событие `Completed` → `Remove()` снимает блок, закрывает окно.

**Параметры.**
```json
{
  "WireCount": 4,
  "SnapRadius": 24,
  "TimeoutSec": 0,
  "PanicHotkey": "Ctrl+Alt+P"
}
```

**Скелет XAML/VM (упрощённо):**
```xaml
<Canvas Background="#66000000">
  <!-- Провода -->
  <ItemsControl ItemsSource="{Binding Wires}">
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <Path Stroke="{Binding Color}" StrokeThickness="6" Data="{Binding PathGeometry}"/>
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
  <!-- Порты -->
  <ItemsControl ItemsSource="{Binding LeftPorts}">
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <Ellipse Width="28" Height="28" Fill="{Binding Color}"
                 MouseLeftButtonDown="Port_MouseDown"/>
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
  <!-- Аналогично RightPorts -->
</Canvas>
```

```csharp
public partial class WiringViewModel : ObservableObject
{
    public ObservableCollection<PortVM> LeftPorts  { get; } = new();
    public ObservableCollection<PortVM> RightPorts { get; } = new();
    public ObservableCollection<WireVM> Wires      { get; } = new();
    [ObservableProperty] private WireVM? currentWire;
    // OnMouseMove: обновляем конец PathGeometry текущего провода
    // OnDrop: если RightPort.Id == LeftPort.Id → фиксируем, иначе отменяем
    // When all matched → Completed?.Invoke()
}
```

---

## 3) PressAllItemsDebuff / PressAllSkillsDebuff — реальные бинды из `.lst`

**Задумка.** Жать **те клавиши**, которые реально настроены у игрока в `dotakeys_personal.lst`.

**Текущая реализация.** Жёсткие массивы `Z...N` и `Q...F`.

**Решение.**
1. Добавить утилиту `DotakeysParser` (KV‑парсер) или использовать уже готовый код парсинга.
2. В момент применения дебафа — дернуть словарь:
   - Items: `Inventory1..Inventory6` → получить `Key` (если пусто — пропуск).
   - Skills: `Ability1..3`, `Ability4/Ultimate`, `Ability5/6`, `D/F` → получить `Key`.
3. Жать полученные `VK` последовательно (между нажатиями 40–60 мс).

**Скелет:**
```csharp
var binds = DotakeysParser.Load(File.ReadAllText(lstPath)); // Name->Key
int Vk(string s) => KeyTranslator.ToVk(s);
foreach (var slot in new[]{"Inventory1","Inventory2","Inventory3","Inventory4","Inventory5","Inventory6"})
{
    if (binds.TryGetValue(slot, out var k) && k.Length>0)
    {
        InputHookHost.Instance.SendKey(Vk(k));
        Thread.Sleep(50);
    }
}
```

---

## 4) BuyTeleportsDebuff — покупка 5 TP «на любом разрешении»

**Задумка.** Купить 5 «Свитков телепортации» и нажать «Доставить курьером», независимо от разрешения/DPI.

**Текущая реализация.** Клик по фиксированным координатам → ломается на 4K и при другом UI‑скейле.

**Решение A (надёжно).**
- Захватить область магазина из окна Dota (`FindWindow` → клиентская область).
- Найти TP‑иконку и кнопку «доставка курьером» **по шаблону** через `OpenCvSharp` (`Cv2.MatchTemplate`).
- Кликнуть по найденной позиции 5 раз, затем по кнопке доставки. Вернуть курсор на исходную точку.

**Решение B (упрощённо).**
- Использовать относительные координаты к клиентской области окна и известный UI‑scale (менее надёжно).

**Библиотеки.** `OpenCvSharp4` (NuGet).

**Псевдокод:**
```csharp
var hwnd = WindowHelper.FindDotaWindow();
var rect = WindowHelper.GetClientRect(hwnd);
using var frame = ScreenCapture.Capture(hwnd, rectShop);
var tp = TemplateFinder.Find(frame, "tp_icon.png", 0.88);
if (tp.Found) Click(rectShop.Left + tp.Center.X, rectShop.Top + tp.Center.Y);
...
```

---

## 5) InputLagDebuff — плавная задержка 0.33s

**Задумка.** Курсор двигается **плавно**, но с задержкой ~330 мс.

**Текущая реализация.** Лаг включается на уровне хуков, но без выверенного воспроизведения буфера (возможны рывки).

**Решение.**
- В `InputHookHost` складывать `WM_MOUSEMOVE` в очередь с временной меткой (ticks).
- Отдельный таймер (120 Гц) «проигрывает» события, чьи метки времени старше `Delay`.
- Между соседними точками — интерполяция (N шагов), чтобы перемещение выглядело гладким.

**Скелет:**
```csharp
record MouseSample(int X, int Y, long Ticks);
ConcurrentQueue<MouseSample> q = new();
Stopwatch sw = Stopwatch.StartNew();
const double DelayMs = 330;

void HookMove(int x,int y) => q.Enqueue(new MouseSample(x,y,sw.ElapsedTicks));

void PlaybackTick()
{
    var target = sw.ElapsedMilliseconds - DelayMs;
    while (q.TryPeek(out var s) && (s.Ticks/TimeSpan.TicksPerMillisecond) <= target)
    {
        q.TryDequeue(out s);
        SmoothMoveTo(s.X, s.Y);
    }
}
```

---

## 6) Общие требования

- **Whitelist паник‑комбо** (например, `Ctrl+Alt+P`) в LL‑хуках — не блокировать эту комбинацию.
- **Идемпотентность**: повторный `Apply()` не должен усиливать эффект (вешать второй хук, блокировать повторно и т.д.).
- **Откат**: системные параметры (скорость мыши и т.п.) восстанавливать в `Remove()` и при аварийном завершении (`AppDomain.ProcessExit`).
- **DPI aware**: оверлеи и клики считать в устройствах окна Dota, не всего экрана.

---

## 7) Приоритет на ближайший PR

1. `PressAllItemsDebuff` / `PressAllSkillsDebuff` → переключить на бинды из `.lst`.
2. `MiniGameDebuff` → полнофункциональная проводка (Canvas + Bezier + snap) + блок ввода с whitelist.
3. `InputLagDebuff` → буферизация с интерполяцией, cap на буфер (600 мс).
4. `BuyTeleportsDebuff` → OpenCV‑поиск TP/Deliver (два шаблона), аккуратный возврат курсора.
5. `RainbowDebuff` → PoC на `Vortice.Windows` (Hue Shift шейдер).

