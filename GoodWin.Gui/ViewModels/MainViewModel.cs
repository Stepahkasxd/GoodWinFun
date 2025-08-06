using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodWin.Core;
using GoodWin.Tracker;
using GoodWin.Gui.Services;
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
        private readonly DebuffsRegistry _registry = new();
        private readonly DebuffScheduler _scheduler = new();
        private readonly DotaConfigService _configService = new();
        private readonly DispatcherTimer _timer;
        private bool _debuffActive;

        public ObservableCollection<string> EventLog { get; } = new();
        public ObservableCollection<IDebuff> AllDebuffs { get; } = new();
        public ObservableCollection<string> DebugLog => DebugLogService.Entries;

        [ObservableProperty] private IDebuff? selectedDebuff;
        partial void OnSelectedDebuffChanged(IDebuff? value) => TestDebuffCommand.NotifyCanExecuteChanged();

        [ObservableProperty] private bool easyEnabled = true;
        [ObservableProperty] private bool mediumEnabled = true;
        [ObservableProperty] private bool hardEnabled = true;
        partial void OnEasyEnabledChanged(bool value) => UpdateDebuffFilters();
        partial void OnMediumEnabledChanged(bool value) => UpdateDebuffFilters();
        partial void OnHardEnabledChanged(bool value) => UpdateDebuffFilters();

        public bool AnyCategoryEnabled => EasyEnabled || MediumEnabled || HardEnabled;

        [ObservableProperty] private string gsiStatus = "GSI не запущен";

        [ObservableProperty] private bool isDotaRunning;

        [ObservableProperty] private string? configPath;
        partial void OnConfigPathChanged(string? value) => InitConfigCommand.NotifyCanExecuteChanged();

        [ObservableProperty] private bool canInitCommands;

        public IRelayCommand TestDebuffCommand { get; }
        public IRelayCommand StartDotaCommand { get; }
        public IRelayCommand BrowseConfigCommand { get; }
        public IRelayCommand InitConfigCommand { get; }
        public IAsyncRelayCommand InitCommandsCommand { get; }

        private HeroDetector? _heroDetector;
        private Guid _heroOverlayId;
        private System.Drawing.Point _heroPoint;

        [ObservableProperty]
        private bool isHeroTrackingEnabled;
        partial void OnIsHeroTrackingEnabledChanged(bool value) => ToggleHeroTracking(value);

        public MainViewModel()
        {
            _listener = new GsiListenerService(3000);
            _listener.OnNewGameState += gs =>
            {
                var clock = gs.Map?.ClockTime;
                if (clock == null)
                {
                    DebugLogService.Log("GSI map data missing");
                    return;
                }
                _scheduler.Update(clock.Value);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    EventLog.Add(DateTime.Now.ToString("T") + " - событие");
                    while (EventLog.Count > 5) EventLog.RemoveAt(0);
                    GsiStatus = "GSI активен";
                });
            };
            _listener.Start();

            _scheduler.DebuffSelectionPending += (s, e) => OnDebuffSelectionPending();

            LoadDebuffs();
            UpdateDebuffFilters();

            TestDebuffCommand = new RelayCommand(TestDebuff, () => SelectedDebuff != null);
            StartDotaCommand = new RelayCommand(StartDota);
            BrowseConfigCommand = new RelayCommand(BrowseConfig);
            InitConfigCommand = new RelayCommand(InitConfigs, () => !string.IsNullOrWhiteSpace(ConfigPath));
            InitCommandsCommand = new AsyncRelayCommand(InitCommands, () => CanInitCommands);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _timer.Tick += (s, e) => CheckDotaProcess();
            _timer.Start();
            CheckDotaProcess();
        }

        private void BrowseConfig()
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
                ConfigPath = dialog.SelectedPath;
        }

        private void InitConfigs()
        {
            if (string.IsNullOrWhiteSpace(ConfigPath)) return;
            if (!_configService.ConfigsExist(ConfigPath))
            {
                _configService.InitializeConfigs(ConfigPath);
            }
            _listener.GenerateConfig();
            CanInitCommands = _configService.ConfigsExist(ConfigPath);
            InitCommandsCommand.NotifyCanExecuteChanged();
        }

        private async Task InitCommands()
        {
            if (string.IsNullOrWhiteSpace(ConfigPath)) return;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _configService.InitializeCommandsAsync(ConfigPath, cts.Token);
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
                    if (Activator.CreateInstance(t) is IDebuff deb)
                        _registry.Register(deb);
                }
                catch (Exception ex)
                {
                    DebugLogService.Log($"Debuff {t.Name} failed to load: {ex.Message}");
                }
            }
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
            if (SelectedDebuff != null && !AllDebuffs.Contains(SelectedDebuff))
                SelectedDebuff = null;

            _scheduler.SetEnabledStages(EasyEnabled, MediumEnabled, HardEnabled);
            OnPropertyChanged(nameof(AnyCategoryEnabled));
        }

        private async void TestDebuff()
        {
            if (SelectedDebuff == null) return;
            var notify = new DebuffNotificationWindow(SelectedDebuff.Name, "Описание дебаффа");
            notify.Show();
            await Task.Delay(3000);
            notify.Close();
            SelectedDebuff.Apply();
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
            StartDebuff(entry);
        }

        private async void StartDebuff(ScheduledDebuffEntry entry)
        {
            var notify = new DebuffNotificationWindow(entry.Debuff.Name, "Описание дебаффа");
            notify.Show();
            await Task.Delay(3000);
            notify.Close();
            RunDebuff(entry);
        }

        private async void RunDebuff(ScheduledDebuffEntry entry)
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
}
