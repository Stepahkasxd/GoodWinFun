using System.Collections.Generic;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodWin.Keybinds;

namespace GoodWin.Gui.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IKeybindService _keybinds;
        private readonly List<KeybindItemViewModel> _allItems = new();

        public ObservableCollection<CategoryViewModel> Categories { get; } = new();

        [ObservableProperty]
        private CategoryViewModel? selectedCategory;

        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand ReloadCommand { get; }

        public SettingsViewModel()
        {
            _keybinds = new KeybindService(new SteamPathService());
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            ReloadCommand = new RelayCommand(() => _keybinds.Reload());
            Load();
            _keybinds.BindingsChanged += (s, e) => Load();
        }

        private void Load()
        {
            Categories.Clear();
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
}
