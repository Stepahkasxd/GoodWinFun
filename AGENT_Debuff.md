# AGENT_Debuff.md — мини‑игра «Протяни провод» (WPF, .NET 8, MVVM)

> Цель: реализовать мини‑игру как отдельный модуль/дебаф для приложения на Windows (net8.0‑windows, WPF) с пакетом `CommunityToolkit.Mvvm (8.2.1)` и `Extended.Wpf.Toolkit (4.5.2)`. По умолчанию 4 провода, 10 секунд на решение. При истечении времени автоматически открываем игровую консоль по клавише `\`, вводим `disconnect` и жмём Enter.

---

## 1) Архитектура и состав проекта

**Решение** `WiresGame.sln`  
**Проект** `WiresGame` (WPF, net8.0-windows)

**Зависимости**
- `CommunityToolkit.Mvvm` 8.2.1 (MVVM, `ObservableObject`, `RelayCommand`)
- `Extended.Wpf.Toolkit` 4.5.2 (опционально: эффекты/контролы, ProgressBar, фонарик и т.п.)

**Папки**
```
/Assets         # стили, изображения (иконки клемм, фон панели)
/Core           # общие утилиты (Random, Geometry helpers)
/Models         # WireModel, JackModel, GameOptions
/ViewModels     # GameViewModel, WireViewModel, JackViewModel
/Views          # GameView.xaml, JackView.xaml
/Services       # DragService, TimerService, ConsoleInvoker
```

---

## 2) Игровая механика (как в Among Us)

- Слева и справа — **по 4 разъёма** (джеков). Каждый джек имеет **метку** (символ/цвет).  
- Слева джек «источник», справа — «приёмник» той же метки. Игрок **перетягивает свободный конец провода** от левого джека к нужному правому.
- Пересечения проводов разрешены, порядок не важен.  
- Победа: все провода соединены с совпадающими метками.  
- Таймер: 10 сек. Если время вышло — **триггер «штрафа»** (см. §6).

---

## 3) Модели и состояние

```csharp
// Models/WireModel.cs
public sealed class WireModel
{
    public required string Id { get; init; }         // "wire-1"
    public required string Tag { get; init; }        // метка: "O", "X", "△", "★" (или "Yellow", "Blue"...)
    public required Color Color { get; init; }       // визуальный цвет провода
    public JackModel LeftJack { get; init; } = null!;
    public JackModel? RightJack { get; set; }        // назначенный правый джек (null, пока не присоединён)
}

// Models/JackModel.cs
public sealed class JackModel
{
    public required string Id { get; init; }         // "L1", "R1"
    public required bool IsLeft { get; init; }
    public required string Tag { get; init; }        // та же метка, что и у провода-пары
    public Point Anchor { get; set; }                // точка на Canvas (автоматически выставляется)
    public bool IsOccupied { get; set; }
}

// Models/GameOptions.cs
public sealed class GameOptions
{
    public int WiresCount { get; init; } = 4;
    public TimeSpan TimeLimit { get; init; } = TimeSpan.FromSeconds(10);
    public bool SnapOnlyMatchingTag { get; init; } = true;  // прищёлкиваем только к совпадающей метке
    public double SnapRadius { get; init; } = 28;           // радиус «магнита» для прищёлкивания
    public Key ConsoleKey { get; init; } = Key.Oem5;        // "\": Oem5
    public string ConsoleCommand { get; init; } = "disconnect";
}
```

---

## 4) ViewModels (MVVM Toolkit)

```csharp
// ViewModels/WireViewModel.cs
public partial class WireViewModel : ObservableObject
{
    public WireModel Model { get; }
    public Point Start => Model.LeftJack.Anchor;

    [ObservableProperty] private Point end;           // текущая тянущаяся точка (курсор или правый джек)
    [ObservableProperty] private bool isDragging;

    public WireViewModel(WireModel model) 
    { 
        Model = model; 
        End = Start; 
    }

    public void AttachTo(JackModel rightJack)
    {
        Model.RightJack = rightJack;
        End = rightJack.Anchor;
    }
}

// ViewModels/GameViewModel.cs
public partial class GameViewModel : ObservableRecipient
{
    private readonly TimerService _timer;
    private readonly ConsoleInvoker _console;

    public ObservableCollection<JackModel> LeftJacks { get; } = new();
    public ObservableCollection<JackModel> RightJacks { get; } = new();
    public ObservableCollection<WireViewModel> Wires { get; } = new();

    [ObservableProperty] private TimeSpan timeLeft;
    [ObservableProperty] private bool isCompleted;
    [ObservableProperty] private bool isFailed;

    public GameOptions Options { get; }

    public GameViewModel(GameOptions? options = null,
                         TimerService? timer = null,
                         ConsoleInvoker? console = null)
    {
        Options = options ?? new();
        _timer = timer ?? new TimerService();
        _console = console ?? new ConsoleInvoker(Options);

        Reset();
    }

    [RelayCommand] private void Reset()
    {
        IsCompleted = false;
        IsFailed = false;
        InitBoard();
        TimeLeft = Options.TimeLimit;
        _timer.Start(Options.TimeLimit, tick => TimeLeft = tick, OnTimeout);
    }

    private void OnTimeout()
    {
        if (IsCompleted) return;
        IsFailed = true;
        _console.TriggerDisconnect();
    }

    private void InitBoard()
    {
        LeftJacks.Clear(); RightJacks.Clear(); Wires.Clear();

        var palette = new (string tag, Color color)[]
        {
            ("O", Colors.Yellow),
            ("X", Colors.RoyalBlue),
            ("△", Colors.OrangeRed),
            ("★", Colors.HotPink),
        }.Take(Options.WiresCount).ToArray();

        // создаём левые/правые джек‑модели
        for (int i=0; i<palette.Length; i++)
        {
            var (tag, color) = palette[i];
            var lj = new JackModel { Id=$"L{i}", IsLeft=true, Tag=tag };
            var rj = new JackModel { Id=$"R{i}", IsLeft=false, Tag=tag };
            LeftJacks.Add(lj);
            RightJacks.Add(rj);
            Wires.Add(new WireViewModel(new WireModel
            {
                Id = $"wire-{i}",
                Tag = tag,
                Color = color,
                LeftJack = lj
            }));
        }

        // перемешиваем порядок правых джеков
        RightJacks.ShuffleInPlace();
    }

    public void TrySnap(WireViewModel wire, Point cursor)
    {
        // найти ближайший свободный правый джек
        var candidate = RightJacks
            .Where(j => !j.IsOccupied)
            .Select(j => (j, dist: (cursor - j.Anchor).Length))
            .OrderBy(t => t.dist)
            .FirstOrDefault();

        if (candidate.j is null) { wire.End = cursor; return; }

        if (candidate.dist <= Options.SnapRadius &&
            (!Options.SnapOnlyMatchingTag || candidate.j.Tag == wire.Model.Tag))
        {
            candidate.j.IsOccupied = true;
            wire.AttachTo(candidate.j);
            CheckWin();
        }
        else
        {
            wire.End = cursor; // просто тянем
        }
    }

    private void CheckWin()
    {
        if (Wires.All(w => w.Model.RightJack is not null &&
                           w.Model.RightJack.Tag == w.Model.Tag))
        {
            IsCompleted = true;
            _timer.Stop();
        }
    }
}
```

**Вспомогалки**

```csharp
// Core/Extensions.cs
public static class Extensions
{
    private static readonly Random _rng = new();
    public static void ShuffleInPlace<T>(this IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        { int j = _rng.Next(i + 1); (list[i], list[j]) = (list[j], list[i]); }
    }
}
```

---

## 5) Представления (XAML)

### 5.1 Общий вид (темная панель, провода — `Path` со сглаживанием)

```xml
<!-- Views/GameView.xaml -->
<UserControl x:Class="WiresGame.Views.GameView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d">
  <Grid Background="#111318" SnapsToDevicePixels="True">
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="120"/>
      <ColumnDefinition Width="*"/>
      <ColumnDefinition Width="120"/>
    </Grid.ColumnDefinitions>

    <!-- Таймер сверху -->
    <Border Grid.ColumnSpan="3" Background="#181B22" Padding="8" CornerRadius="6" Margin="12">
      <DockPanel>
        <TextBlock Text="ПРОТЯНИ ПРОВОД" Foreground="#C9D1D9" FontWeight="SemiBold" />
        <ProgressBar DockPanel.Dock="Right" Width="180" Height="14"
                     Value="{Binding TimeLeft.TotalMilliseconds}"
                     Maximum="{Binding Options.TimeLimit.TotalMilliseconds}"
                     Margin="12,0,0,0"/>
        <TextBlock DockPanel.Dock="Right" Foreground="#F0F6FC"
                   Text="{Binding TimeLeft, StringFormat={}{0:ss\.ff}}"/>
      </DockPanel>
    </Border>

    <!-- Левые джеки -->
    <ItemsControl Grid.Column="0" ItemsSource="{Binding LeftJacks}" Margin="8,48,8,8">
      <ItemsControl.ItemTemplate>
        <DataTemplate>
          <Border Height="48" Margin="0,8"
                  Background="#1E222B" CornerRadius="8"
                  BorderBrush="#2A2F3A" BorderThickness="1">
            <DockPanel>
              <TextBlock Text="{Binding Tag}" FontSize="24" Foreground="#E6EDF3"
                         VerticalAlignment="Center" Margin="12,0"/>
              <!-- Крепёж под якорь: захватим координату через Loaded -->
              <Ellipse Width="14" Height="14" Fill="#E6EDF3" Margin="8"
                       HorizontalAlignment="Right" VerticalAlignment="Center"
                       Loaded="LeftAnchor_Loaded"/>
            </DockPanel>
          </Border>
        </DataTemplate>
      </ItemsControl.ItemTemplate>
    </ItemsControl>

    <!-- Правые джеки -->
    <ItemsControl Grid.Column="2" ItemsSource="{Binding RightJacks}" Margin="8,48,8,8">
      <ItemsControl.ItemTemplate>
        <DataTemplate>
          <Border Height="48" Margin="0,8"
                  Background="#1E222B" CornerRadius="8"
                  BorderBrush="#2A2F3A" BorderThickness="1">
            <DockPanel>
              <Ellipse Width="14" Height="14" Fill="#E6EDF3" Margin="8"
                       VerticalAlignment="Center" Loaded="RightAnchor_Loaded"/>
              <TextBlock Text="{Binding Tag}" FontSize="24" Foreground="#E6EDF3"
                         VerticalAlignment="Center" Margin="12,0"/>
            </DockPanel>
          </Border>
        </DataTemplate>
      </ItemsControl.ItemTemplate>
    </ItemsControl>

    <!-- Полотно с проводами -->
    <Canvas Grid.Column="1" ClipToBounds="True" x:Name="WiresCanvas">
      <ItemsControl ItemsSource="{Binding Wires}">
        <ItemsControl.ItemsPanel>
          <ItemsPanelTemplate><Canvas/></ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <Path StrokeThickness="10" StrokeStartLineCap="Round" StrokeEndLineCap="Round">
              <Path.Stroke>
                <SolidColorBrush Color="{Binding Model.Color}"/>
              </Path.Stroke>
              <Path.Data>
                <!-- Реальную геометрию мы подменяем из code-behind -->
                <PathGeometry/>
              </Path.Data>
            </Path>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </Canvas>
  </Grid>
</UserControl>
```

> **Обработчики в code-behind** (`GameView.xaml.cs`) нужны только для **захвата координат якорей** (левый/правый джек) и **подписки на события мыши** для перетягивания. Вся логика принятия решений — во ViewModel.

### 5.2 Code-behind (минимум, только UI‑события)

```csharp
public partial class GameView : UserControl
{
    public GameView() { InitializeComponent(); DataContextChanged += OnDataContextChanged; }

    private GameViewModel VM => (GameViewModel)DataContext;

    // Проставляем Anchor у джеков, когда отрисовались
    private void LeftAnchor_Loaded(object sender, RoutedEventArgs e)
    {
        var el = (FrameworkElement)sender;
        var jack = (JackModel)el.DataContext;
        var pt = el.TranslatePoint(new Point(el.ActualWidth/2, el.ActualHeight/2), WiresCanvas);
        jack.Anchor = pt;
    }

    private void RightAnchor_Loaded(object sender, RoutedEventArgs e)
    {
        var el = (FrameworkElement)sender;
        var jack = (JackModel)el.DataContext;
        var pt = el.TranslatePoint(new Point(el.ActualWidth/2, el.ActualHeight/2), WiresCanvas);
        jack.Anchor = pt;
    }

    private void OnDataContextChanged(object? s, DependencyPropertyChangedEventArgs e)
    {
        WiresCanvas.MouseMove += (s2, a) =>
        {
            if (VM.Wires.FirstOrDefault(w => w.IsDragging) is { } wvm)
            {
                var p = a.GetPosition(WiresCanvas);
                wvm.End = p;
            }
        };

        WiresCanvas.MouseLeftButtonUp += (s3, a) =>
        {
            if (VM.Wires.FirstOrDefault(w => w.IsDragging) is { } wvm)
            {
                var p = a.GetPosition(WiresCanvas);
                wvm.IsDragging = false;
                VM.TrySnap(wvm, p);
            }
        };
    }

    // Визуальная Безье: P0 = Start, P3 = End; P1/P2 — автоконтроль
    private void RedrawWire(Path path, WireViewModel vm)
    {
        var p0 = vm.Start; var p3 = vm.End;
        var dx = Math.Abs(p3.X - p0.X) * 0.6;
        var p1 = new Point(p0.X + dx, p0.Y);
        var p2 = new Point(p3.X - dx, p3.Y);

        var geom = new PathGeometry();
        var fig  = new PathFigure { StartPoint = p0, IsClosed = false };
        fig.Segments.Add(new BezierSegment { Point1 = p1, Point2 = p2, Point3 = p3, IsStroked = true });
        geom.Figures.Add(fig);
        path.Data = geom;
    }

    private void Path_Loaded(object sender, RoutedEventArgs e)
    {
        var path = (Path)sender;
        var vm = (WireViewModel)path.DataContext;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(WireViewModel.End) or nameof(WireViewModel.IsDragging))
                RedrawWire(path, vm);
        };
        RedrawWire(path, vm);
        path.MouseLeftButtonDown += (_, __) => vm.IsDragging = true;
    }
}
```

---

## 6) Таймер и штраф (авто‑`disconnect`)

### 6.1 Сервис таймера

```csharp
// Services/TimerService.cs
public sealed class TimerService
{
    private DispatcherTimer? _timer;
    private DateTime _end;
    private Action<TimeSpan>? _onTick;
    private Action? _onTimeout;

    public void Start(TimeSpan duration, Action<TimeSpan> onTick, Action onTimeout)
    {
        Stop();
        _end = DateTime.UtcNow + duration;
        _onTick = onTick;
        _onTimeout = onTimeout;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            var left = _end - DateTime.UtcNow;
            if (left <= TimeSpan.Zero)
            { Stop(); _onTick?.Invoke(TimeSpan.Zero); _onTimeout?.Invoke(); }
            else _onTick?.Invoke(left);
        };
        _timer.Start();
    }

    public void Stop() { _timer?.Stop(); _timer = null; }
}
```

### 6.2 Вызов игровой консоли и ввод команды

Без внешних библиотек используем WinAPI `SendInput` и, при необходимости, фокусируем окно игры (`SetForegroundWindow`). Горячая клавиша `\` — это `Key.Oem5`.

> **Важно:** посылать ввод можно **только в активное окно**. Если приложение не в фокусе, сначала активируем окно игры (по заголовку/классу). Ниже — две стратегии.

#### Вариант A. «Слепая» отправка в текущее активное окно
Подходит, если мини‑игра запускается **поверх Dota 2** (или встраивается в оверлей), и фокус остаётся у игры.

```csharp
// Services/ConsoleInvoker.cs
public sealed class ConsoleInvoker
{
    private readonly GameOptions _opt;
    public ConsoleInvoker(GameOptions opt) => _opt = opt;

    public void TriggerDisconnect()
    {
        // key "" (toggle console)
        SendKey(_opt.ConsoleKey);
        // текст команды
        SendText(_opt.ConsoleCommand);
        // Enter
        SendKey(Key.Enter);
    }

    // --- реализация SendInput (упрощённая) ---
    public static void SendKey(Key key)
    {
        var vkey = KeyInterop.VirtualKeyFromKey(key);
        var inputs = new INPUT[]
        {
            INPUT.KeyDown((ushort)vkey),
            INPUT.KeyUp((ushort)vkey),
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    public static void SendText(string text)
    {
        foreach (var ch in text)
        {
            short vk = VkFromChar(ch, out bool shift);
            if (shift) SendKey(Key.LeftShift);
            SendKey((Key)KeyInterop.KeyFromVirtualKey(vk));
            if (shift) SendKey(Key.LeftShift); // отпускание шифта
        }
    }

    private static short VkFromChar(char c, out bool shift)
    {
        var vk = VkKeyScan(c);
        shift = (vk >> 8) == 1;
        return (short)(vk & 0xff);
    }

    #region PInvoke
    [DllImport("user32.dll")] static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] static extern short VkKeyScan(char ch);

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public uint type; public INPUTUNION u;
        public static INPUT KeyDown(ushort vk) => new() { type = 1, u = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk } } };
        public static INPUT KeyUp(ushort vk)   => new() { type = 1, u = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = 2 } } };
    }
    [StructLayout(LayoutKind.Explicit)] struct INPUTUNION { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    #endregion
}
```

#### Вариант B. Фокусируем окно Dota 2 перед отправкой
Если мини‑игра находится в отдельном окне.

```csharp
// Дополнительно внутри ConsoleInvoker
[DllImport("user32.dll", SetLastError=true)]
static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
[DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);

private static bool FocusDotaWindow()
{
    // Часто заголовок содержит "Dota 2"
    var h = FindWindow(null, "Dota 2");
    if (h == IntPtr.Zero) return false;
    return SetForegroundWindow(h);
}

public void TriggerDisconnectSafe()
{
    if (FocusDotaWindow())
        TriggerDisconnect();
}
```

> Если допустимы внешние пакеты, можно заменить всё на `WindowsInput` (InputSimulator) — код станет компактнее.

---

## 7) Логика drag & drop (детали UX)

- Захват провода — кликом по любой его части (`Path.MouseLeftButtonDown` → `IsDragging=true`).
- Пока тащим, `End` следует за курсором, Безье пересчитывается.
- Отпустили мышь (`MouseLeftButtonUp`) → `TrySnap`.  
  Если найден свободный правый джек **в пределах SnapRadius** и (опционально) с совпадающей меткой — провод **прищёлкивается**. Иначе остаётся висеть концом в точке отпускания; повторный захват разрешён.
- Дополнительно можно делать **подсветку** ближайшей совместимой клеммы (через `Triggers` и `AdornerLayer`).

---

## 8) Стили и визуал

- Фон панели — #111318, карточки джеков — #1E222B, граница #2A2F3A.  
- Цвета проводов (по умолчанию): Yellow `#FFD60A`, Blue `#2952E3`, Red `#FF3B30` (OrangeRed), Pink `#FF4D9E`.
- Толщина провода `10px`, скруглённые концы (`Stroke*Cap=Round`).
- При наведении на совместимый правый джек — лёгкое свечение (`DropShadowEffect` у контейнера).

---

## 9) Инициализация в приложении

```xml
<!-- App.xaml -->
<Application ... StartupUri="MainWindow.xaml">
  <Application.Resources>
    <!-- Место для глобальных стилей -->
  </Application.Resources>
</Application>
```

```xml
<!-- MainWindow.xaml -->
<Window ... Title="Debuff: Протяни провод" Width="960" Height="600" ResizeMode="NoResize">
  <views:GameView/>
</Window>
```

```csharp
// MainWindow.xaml.cs
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new GameViewModel(new GameOptions
        {
            WiresCount = 4,
            TimeLimit = TimeSpan.FromSeconds(10),
            ConsoleKey = Key.Oem5,
            ConsoleCommand = "disconnect"
        });
    }
}
```

---

## 10) Проверка/отладка

1. При запуске видим 4 левых и 4 правых джека с перемешанным порядком справа.  
2. Перетаскивание любого провода — работает; при отпускании рядом с «правильной» клеммой — прищёлкивание.  
3. Таймер идёт 10 секунд, по нулю — вызывается `ConsoleInvoker.TriggerDisconnect()`.  
4. При правильном соединении всех проводов — `IsCompleted=true`, таймер останавливается.

**Юнит‑точки** (без UI):
- `GameViewModel.InitBoard()` создаёт корректные пары.
- `TrySnap()` — прищёлкивает только в радиусе и (если включено) по совпадению метки.
- `CheckWin()` — возвращает победу только при идеальных совпадениях.

---

## 11) Расширения на будущее

- Режимы сложности: 5–6 проводов, меньший `SnapRadius`, случайные символы.  
- «Искры» при соединении (ParticleSystem на `CompositionTarget.Rendering`).  
- Звуки: connect/fail/tick (через `MediaPlayer`).  
- Мульти‑платформенный инпут (Raw Input, Low‑Level Keyboard Hook) для стабильного отправления команд в игру.  
- Интеграция как **дебаф**: выдаём `GameView` как `UserControl` и API:
  ```csharp
  public interface IWireDebuff
  {
      UserControl View { get; }                     // вставляется в оверлей/окно
      event EventHandler Completed;                 // успех
      event EventHandler Failed;                    // истёк таймер → применить наказание
      void Start(GameOptions options);
      void Stop();
  }
  ```

---

## 12) Минимальный `csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.1" />
    <PackageReference Include="Extended.Wpf.Toolkit" Version="4.5.2" />
  </ItemGroup>
</Project>
```

---

## 13) Юридические нюансы

Мы **копируем механику** (соединение проводов и тайм‑предел), но используем **собственную графику и звуки**. Так мы избегаем проблем с IP Among Us.

---

## 14) Чек‑лист интеграции как дебаф

- [ ] Создан модуль/проект `WiresGame` с зависимостями.  
- [ ] `GameView` вставлен в ваше окно дебафов/оверлей.  
- [ ] Таймер = 10 сек, по тайм‑ауту — вызов `ConsoleInvoker.TriggerDisconnect()`.  
- [ ] Параметры (`WiresCount`, `TimeLimit`, `ConsoleKey`, `ConsoleCommand`) прокидываются извне.  
- [ ] Обработчики событий `Completed`/`Failed` подключены к вашей общей системе дебафов.
