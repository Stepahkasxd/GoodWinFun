using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodWin.Keybinds;
using GoodWin.Gui.Services;
using Microsoft.Win32;

namespace GoodWin.Gui.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IKeybindService _keybinds;
        private readonly List<KeybindItemViewModel> _allItems = new();

        public ObservableCollection<CategoryViewModel> Categories { get; } = new();
        public ObservableCollection<PresetInfo> Presets { get; } = new();

        [ObservableProperty]
        private CategoryViewModel? selectedCategory;

        [ObservableProperty]
        private PresetInfo? selectedPreset;

        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand ReloadCommand { get; }
        public IAsyncRelayCommand ExportCommand { get; }
        public IAsyncRelayCommand ImportCommand { get; }
        public IRelayCommand ApplyPresetCommand { get; }

        public SettingsViewModel()
        {
            _keybinds = new KeybindService(new SteamPathService());
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            ReloadCommand = new RelayCommand(() => _keybinds.Reload());
            ExportCommand = new AsyncRelayCommand(ExportAsync);
            ImportCommand = new AsyncRelayCommand(ImportAsync);
            ApplyPresetCommand = new RelayCommand(ApplyPreset, () => SelectedPreset != null);
            Load();
            LoadPresets();
            _keybinds.BindingsChanged += (s, e) => Load();
        }

        partial void OnSelectedPresetChanged(PresetInfo? value) => ApplyPresetCommand.NotifyCanExecuteChanged();

        private void Load()
        {
            Categories.Clear();
            foreach (var item in _allItems)
                item.PropertyChanged -= Item_PropertyChanged;
            _allItems.Clear();
            foreach (var item in _keybinds.Entries.Select(e => new KeybindItemViewModel(e)))
            {
                item.PropertyChanged += Item_PropertyChanged;
                _allItems.Add(item);
            }
            var groups = _allItems.GroupBy(i => i.Category).OrderBy(g => g.Key);
            foreach (var g in groups)
                Categories.Add(new CategoryViewModel(g.Key, g));
            SelectedCategory = Categories.FirstOrDefault();
            UpdateConflicts();
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeybindItemViewModel.Key))
                UpdateConflicts();
        }

        private void UpdateConflicts()
        {
            foreach (var item in _allItems)
                item.HasConflict = false;
            var groups = _allItems.GroupBy(i => i.Key).Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1);
            foreach (var g in groups)
                foreach (var item in g)
                    item.HasConflict = true;
        }

        private async Task SaveAsync()
        {
            var entries = _allItems.Select(i => { i.Model.Key = i.Key; return i.Model; }).ToList();
            await _keybinds.SaveAsync(entries);
        }

        private async Task ExportAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files|*.json|All files|*.*",
                FileName = "keybinds.json"
            };
            if (dialog.ShowDialog() != true) return;
            var entries = _allItems.Select(i => { i.Model.Key = i.Key; return i.Model; }).ToList();
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(dialog.FileName, json);
        }

        private async Task ImportAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files|*.json|All files|*.*"
            };
            if (dialog.ShowDialog() != true) return;
            if (MessageBox.Show("Overwrite current keybinds?", "Import", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                var text = await File.ReadAllTextAsync(dialog.FileName);
                var entries = JsonSerializer.Deserialize<List<KeybindEntry>>(text) ?? new();
                foreach (var entry in entries)
                {
                    var item = _allItems.FirstOrDefault(i => string.Equals(i.Model.Label, entry.Label, StringComparison.OrdinalIgnoreCase));
                    if (item != null)
                        item.Key = entry.Key ?? string.Empty;
                }
                UpdateConflicts();
                await SaveAsync();
            }
            catch (Exception ex)
            {
                DebugLogService.Log($"Import keybinds failed for {dialog.FileName}: {ex.Message}");
            }
        }

        private void ApplyPreset()
        {
            if (SelectedPreset is null) return;
            foreach (var kv in SelectedPreset.Bindings)
            {
                var item = _allItems.FirstOrDefault(i => string.Equals(i.Model.Label, kv.Key, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                    item.Key = kv.Value;
            }
            UpdateConflicts();
        }

        private void LoadPresets()
        {
            Presets.Clear();
            var dir = Path.Combine(AppContext.BaseDirectory, "Presets");
            if (!Directory.Exists(dir)) return;
            foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
            {
                try
                {
                    var text = File.ReadAllText(file);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? new();
                    Presets.Add(new PresetInfo(Path.GetFileNameWithoutExtension(file), dict));
                }
                catch (Exception ex)
                {
                    DebugLogService.Log($"Failed to load preset {file}: {ex.Message}");
                }
            }
        }
    }

    public sealed class CategoryViewModel
    {
        public string Name { get; }
        public ObservableCollection<KeybindItemViewModel> Items { get; }

        public CategoryViewModel(string name, IEnumerable<KeybindItemViewModel> items)
        {
            Name = name;
            Items = new ObservableCollection<KeybindItemViewModel>(items);
        }
    }

    public partial class KeybindItemViewModel : ObservableObject
    {
        public KeybindEntry Model { get; }
        public string Category { get; }
        public string FriendlyName => DotaFriendly.MakeFriendly(Model);

        [ObservableProperty]
        private string key;

        [ObservableProperty]
        private bool hasConflict;

        public KeybindItemViewModel(KeybindEntry m)
        {
            Model = m;
            Category = DotaCategories.ToCategory(m.Panel);
            key = m.Key ?? string.Empty;
        }
    }

    public sealed class PresetInfo
    {
        public string Name { get; }
        public Dictionary<string, string> Bindings { get; }

        public PresetInfo(string name, Dictionary<string, string> bindings)
        {
            Name = name;
            Bindings = bindings;
        }
    }
}
