using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodWin.Core;
using GoodWin.Tracker;
using GoodWin.Gui.Services;
using GoodWin.Keybinds;
using GoodWin.Utils;
using GoodWin.Gui.Views;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoodWin.Gui.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly GsiListenerService _listener;
        private readonly IDotaPathResolver _pathResolver;
        private readonly DebuffsRegistry _registry = new();
        private readonly DebuffScheduler _scheduler = new();
        private readonly DotaCommandService _commandService = new();
        private readonly UserSettingsService _settingsService = new("usersettings.json");
        private readonly DispatcherTimer _timer;
        private readonly IKeybindService _keybindService;
        private bool _debuffActive;

        public ObservableCollection<string> EventLog { get; } = new();
        public ObservableCollection<IDebuff> AllDebuffs { get; } = new();
        public ObservableCollection<string> DebugLog => DebugLogService.Entries;
        public ObservableCollection<PlayerDisplay> Players { get; } = new();
        public ObservableCollection<PathDisplay> Paths { get; } = new();

        [ObservableProperty] private bool easyEnabled = true;
        [ObservableProperty] private bool mediumEnabled = true;
        [ObservableProperty] private bool hardEnabled = true;
        partial void OnEasyEnabledChanged(bool value) => UpdateDebuffFilters();
        partial void OnMediumEnabledChanged(bool value) => UpdateDebuffFilters();
        partial void OnHardEnabledChanged(bool value) => UpdateDebuffFilters();

        public bool AnyCategoryEnabled => EasyEnabled || MediumEnabled || HardEnabled;

        [ObservableProperty] private string gsiStatus = "GSI не запущен";

        [ObservableProperty] private bool isDotaRunning;

        [ObservableProperty] private MatchTeam localTeam;

        public IAsyncRelayCommand<IDebuff> RunDebuffCommand { get; }
        public IRelayCommand StartDotaCommand { get; }
        public IAsyncRelayCommand InitCommandsCommand { get; }

        private HeroDetector? _heroDetector;
        private Guid _heroOverlayId;
        private System.Drawing.Point _heroPoint;

        [ObservableProperty]
        private bool isHeroTrackingEnabled;
        partial void OnIsHeroTrackingEnabledChanged(bool value) => ToggleHeroTracking(value);

        public MainViewModel()
        {
            _pathResolver = new DotaPathResolver();
            _keybindService = new KeybindService(new SteamPathService());
            _listener = new GsiListenerService(_pathResolver, 3000);
            _listener.OnNewMatchState += state =>
            {
                _scheduler.Update(state.Time);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LocalTeam = state.LocalTeam;
                    Players.Clear();
                    foreach (var p in state.Players)
                        Players.Add(new PlayerDisplay(p.Name, p.HeroName, p.Team == state.LocalTeam));
                    EventLog.Add(DateTime.Now.ToString("T") + " - событие");
                    while (EventLog.Count > 5) EventLog.RemoveAt(0);
                    GsiStatus = "GSI активен";
                });
            };
            var cfgPath = _pathResolver.EnsureConfigCreated();
            Paths.Add(new PathDisplay("GSI config", cfgPath ?? "не найден"));
            Paths.Add(new PathDisplay("dotakeys_personal.lst", _keybindService.CurrentPath ?? "не найден"));
            _listener.Start();

            _scheduler.DebuffSelectionPending += (s, e) => OnDebuffSelectionPending();

            LoadDebuffs();
            UpdateDebuffFilters();

            RunDebuffCommand = new AsyncRelayCommand<IDebuff>(RunManualDebuff);
            StartDotaCommand = new RelayCommand(StartDota);
            InitCommandsCommand = new AsyncRelayCommand(InitCommands);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _timer.Tick += (s, e) => CheckDotaProcess();
            _timer.Start();
            CheckDotaProcess();
        }

        private async Task InitCommands()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _commandService.InitializeCommandsAsync(cts.Token);
            }
            catch (Exception ex)
            {
                DebugLogService.Log($"InitCommands failed: {ex.Message}");
            }
        }

        private void LoadDebuffs()
        {
            // load all GoodWin.Debuffs* assemblies from output folder
            try
            {
                var dir = AppContext.BaseDirectory;
                foreach (var file in System.IO.Directory.GetFiles(dir, "GoodWin.Debuffs*.dll"))
                {
                    try { System.Reflection.Assembly.LoadFrom(file); }
                    catch (Exception ex)
                    {
                        DebugLogService.Log($"Failed to load assembly {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Log($"Error scanning debuff assemblies: {ex.Message}");
            }
            // Ensure debuff assemblies are loaded before scanning types
            var referenced = System.Reflection.Assembly
                .GetExecutingAssembly()
                .GetReferencedAssemblies()
                .Where(a => a.Name?.StartsWith("GoodWin.Debuffs") == true);

            foreach (var name in referenced)
            {
                try
                {
                    System.Reflection.Assembly.Load(name);
                }
                catch (Exception ex)
                {
                    DebugLogService.Log($"Failed to load referenced assembly {name.Name}: {ex.Message}");
                }
            }

            var debuffTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name?.StartsWith("GoodWin.Debuffs") == true)
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IDebuff).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var t in debuffTypes)
            {
                try
                {
                    IDebuff? debuff = null;
                    var ctor = t.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault();
                    if (ctor != null)
                    {
                        var parameters = ctor.GetParameters();
                        if (parameters.Length == 0)
                        {
                            debuff = Activator.CreateInstance(t) as IDebuff;
                        }
                        else
                        {
                            var args = new object?[parameters.Length];
                            bool ok = true;
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                if (parameters[i].ParameterType == typeof(IKeybindService))
                                {
                                    args[i] = _keybindService;
                                    continue;
                                }
                                var val = GetSettingValue(parameters[i].Name);
                                if (val == null)
                                {
                                    ok = false;
                                    break;
                                }
                                args[i] = Convert.ChangeType(val, parameters[i].ParameterType);
                            }
                            if (ok)
                                debuff = Activator.CreateInstance(t, args) as IDebuff;
                            else
                                DebugLogService.Log($"Debuff {t.Name} skipped: missing parameters");
                        }
                    }
                    if (debuff != null)
                        _registry.Register(debuff);
                }
                catch (Exception ex)
                {
                    DebugLogService.Log($"Debuff {t.Name} failed to load: {ex.Message}");
                }
            }
        }

        private object? GetSettingValue(string name)
        {
            return GetSettingValueRecursive(_settingsService.Settings, name);
        }

        private object? GetSettingValueRecursive(object obj, string name)
        {
            var props = obj.GetType().GetProperties();
            foreach (var prop in props)
            {
                var val = prop.GetValue(obj);
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    return val;
                if (val != null && !prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
                {
                    var found = GetSettingValueRecursive(val, name);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private void UpdateDebuffFilters()
        {
            AllDebuffs.Clear();
            foreach (var entry in _registry.GetAllEntries())
            {
                if ((entry.Schedule.Phase == DebuffPhase.Easy && EasyEnabled) ||
                    (entry.Schedule.Phase == DebuffPhase.Medium && MediumEnabled) ||
                    (entry.Schedule.Phase == DebuffPhase.Hard && HardEnabled))
                {
                    AllDebuffs.Add(entry.Debuff);
                }
            }

            _scheduler.SetEnabledStages(EasyEnabled, MediumEnabled, HardEnabled);
            OnPropertyChanged(nameof(AnyCategoryEnabled));
        }

        private async Task RunManualDebuff(IDebuff debuff)
        {
            var notify = new DebuffNotificationWindow(debuff.Name, "Описание дебаффа");
            notify.Show();
            await Task.Delay(3000);
            notify.Close();
            var attr = debuff.GetType().GetCustomAttribute<DebuffScheduleAttribute>();
            var duration = attr?.DurationSeconds ?? 60;
            try
            {
                debuff.Apply();
                await Task.Delay(duration * 1000);
            }
            catch (Exception ex)
            {
                DebugLogService.Log($"Manual debuff {debuff.Name} error: {ex.Message}");
            }
            finally
            {
                debuff.Remove();
                EventLog.Add(DateTime.Now.ToString("T") + $" - {debuff.Name} завершён");
            }
        }

        private void StartDota()
        {
            try
            {
                System.Diagnostics.Process.Start("steam://rungameid/570");
            }
            catch { }
        }

        private void CheckDotaProcess()
        {
            IsDotaRunning = System.Diagnostics.Process.GetProcessesByName("dota2").Any();
        }

        private void ToggleHeroTracking(bool enabled)
        {
            if (enabled)
            {
                var capture = new ScreenCaptureService(60);
                var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                var minimapRect = new System.Drawing.Rectangle(0, bounds.Height - 256, 256, 256);
                _heroDetector = new HeroDetector(capture, minimapRect);
                _heroDetector.HeroPositionUpdated += pos =>
                {
                    _heroPoint = pos;
                    OverlayWindow.Instance.Dispatcher.Invoke(() => OverlayWindow.Instance.InvalidateVisual());
                };
                _heroOverlayId = OverlayWindow.Instance.AddOverlay(dc =>
                {
                    var brush = System.Windows.Media.Brushes.Red;
                    dc.DrawEllipse(null, new System.Windows.Media.Pen(brush, 3), new System.Windows.Point(_heroPoint.X, _heroPoint.Y), 15, 15);
                });
                _heroDetector.Start();
            }
            else
            {
                if (_heroDetector != null)
                {
                    _heroDetector.Stop();
                    _heroDetector.Dispose();
                    _heroDetector = null;
                }
                OverlayWindow.Instance.RemoveOverlay(_heroOverlayId);
            }
        }

        private readonly Random _rand = new();
        private void OnDebuffSelectionPending()
        {
            if (_debuffActive)
            {
                _scheduler.Allow();
                return;
            }

            DebuffPhase? phase = _scheduler.CurrentStage switch
            {
                Stage.Easy => DebuffPhase.Easy,
                Stage.Medium => DebuffPhase.Medium,
                Stage.Hard => DebuffPhase.Hard,
                _ => null
            };
            if (phase == null)
            {
                _scheduler.Allow();
                return;
            }

            var entries = _registry.GetAllEntries()
                .Where(e => e.Schedule.Phase == phase)
                .ToList();
            if (entries.Count == 0)
            {
                _scheduler.Allow();
                return;
            }

            var entry = entries[_rand.Next(entries.Count)];
            _ = StartDebuff(entry);
        }

        private async Task StartDebuff(ScheduledDebuffEntry entry)
        {
            var notify = new DebuffNotificationWindow(entry.Debuff.Name, "Описание дебаффа");
            notify.Show();
            await Task.Delay(3000);
            notify.Close();
            try
            {
                await RunDebuff(entry);
            }
            catch (Exception ex)
            {
                DebugLogService.Log($"Debuff {entry.Debuff.Name} error: {ex.Message}");
            }
        }

        private async Task RunDebuff(ScheduledDebuffEntry entry)
        {
            _debuffActive = true;
            entry.Debuff.Apply();
            try
            {
                await Task.Delay(entry.Schedule.DurationSeconds * 1000);
            }
            finally
            {
                entry.Debuff.Remove();
                _debuffActive = false;
                _scheduler.Allow();
            }
        }
    }

    public record PlayerDisplay(string Name, string? HeroName, bool IsAlly);
    public record PathDisplay(string Name, string Path);
}
