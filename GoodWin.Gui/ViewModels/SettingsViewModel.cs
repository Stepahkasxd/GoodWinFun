using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GoodWin.Keybinds;

namespace GoodWin.Gui.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IKeybindService _keybinds;
        public ObservableCollection<BindingDisplay> Bindings { get; } = new();

        public SettingsViewModel()
        {
            _keybinds = new KeybindService(new SteamPathService());
            Load();
            _keybinds.BindingsChanged += (s, e) => Load();
        }

        private void Load()
        {
            Bindings.Clear();
            foreach (var kv in _keybinds.Bindings)
                Bindings.Add(new BindingDisplay(kv.Key, kv.Value));
        }
    }

    public record BindingDisplay(string Label, string Key);
}
