using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodWin.Core;
using GoodWin.Tracker;
using GoodWin.Gui.Services;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoodWin.Gui.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly GsiListenerService _listener;
        private readonly DebuffsRegistry _registry = new();
        private readonly DebuffScheduler _scheduler = new();
        private readonly RouletteService _roulette = new();
        private readonly DotaConfigService _configService = new();
        private readonly DispatcherTimer _timer;
        private bool _debuffActive;

        public ObservableCollection<string> EventLog { get; } = new();
        public ObservableCollection<IDebuff> AllDebuffs { get; } = new();

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
        public IRelayCommand InitCommandsCommand { get; }

        public MainViewModel()
        {
            _listener = new GsiListenerService(3000);
            _listener.OnNewGameState += gs =>
            {
                _scheduler.Update(gs.Map.ClockTime);
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
            InitCommandsCommand = new RelayCommand(InitCommands, () => CanInitCommands);

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

        private void InitCommands()
        {
            if (string.IsNullOrWhiteSpace(ConfigPath)) return;
            Task.Run(() => _configService.InitializeCommands(ConfigPath));
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
                    catch { /* ignore missing assemblies */ }
                }
            }
            catch { }
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
                catch
                {
                    // ignore loading failures
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
                catch
                {
                    // skip debuffs that fail to instantiate or register
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

        private void TestDebuff()
        {
            SelectedDebuff?.Apply();
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

            var events = entries.Select(e => new Event
            {
                Name = e.Debuff.Name,
                Logic = () => RunDebuff(e)
            }).ToList();

            _roulette.ShowRouletteForEvents(events, null);
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
