using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodWin.Gui.Services;

namespace GoodWin.Gui.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly UserSettingsService _service;

        [ObservableProperty] private string firstSkill;
        [ObservableProperty] private string secondSkill;
        [ObservableProperty] private string thirdSkill;
        [ObservableProperty] private string ultimate;
        [ObservableProperty] private string consoleKey;
        [ObservableProperty] private string chatKey;
        [ObservableProperty] private string teamChatKey;

        [ObservableProperty] private string itemSlot1;
        [ObservableProperty] private string itemSlot2;
        [ObservableProperty] private string itemSlot3;
        [ObservableProperty] private string itemSlot4;
        [ObservableProperty] private string itemSlot5;
        [ObservableProperty] private string itemSlot6;

        public IRelayCommand SaveCommand { get; }
        public IRelayCommand ResetCommand { get; }

        public SettingsViewModel()
        {
            _service = new UserSettingsService("usersettings.json");
            var s = _service.Settings;
            firstSkill = s.Controls.Abilities.Slot1;
            secondSkill = s.Controls.Abilities.Slot2;
            thirdSkill = s.Controls.Abilities.Slot3;
            ultimate = s.Controls.Abilities.Ultimate;
            consoleKey = s.Controls.ConsoleKey;
            chatKey = s.Controls.ChatKey;
            teamChatKey = s.Controls.TeamChatKey;
            itemSlot1 = s.Controls.Items.Slot1;
            itemSlot2 = s.Controls.Items.Slot2;
            itemSlot3 = s.Controls.Items.Slot3;
            itemSlot4 = s.Controls.Items.Slot4;
            itemSlot5 = s.Controls.Items.Slot5;
            itemSlot6 = s.Controls.Items.Slot6;
            SaveCommand = new RelayCommand(Save);
            ResetCommand = new RelayCommand(Reset);
        }

        private void Save()
        {
            var s = _service.Settings;
            s.Controls.Abilities.Slot1 = FirstSkill;
            s.Controls.Abilities.Slot2 = SecondSkill;
            s.Controls.Abilities.Slot3 = ThirdSkill;
            s.Controls.Abilities.Ultimate = Ultimate;
            s.Controls.ConsoleKey = ConsoleKey;
            s.Controls.ChatKey = ChatKey;
            s.Controls.TeamChatKey = TeamChatKey;
            s.Controls.Items.Slot1 = ItemSlot1;
            s.Controls.Items.Slot2 = ItemSlot2;
            s.Controls.Items.Slot3 = ItemSlot3;
            s.Controls.Items.Slot4 = ItemSlot4;
            s.Controls.Items.Slot5 = ItemSlot5;
            s.Controls.Items.Slot6 = ItemSlot6;
            _service.Save();
        }

        private void Reset()
        {
            _service.Reset();
            var s = _service.Settings;
            FirstSkill = s.Controls.Abilities.Slot1;
            SecondSkill = s.Controls.Abilities.Slot2;
            ThirdSkill = s.Controls.Abilities.Slot3;
            Ultimate = s.Controls.Abilities.Ultimate;
            ConsoleKey = s.Controls.ConsoleKey;
            ChatKey = s.Controls.ChatKey;
            TeamChatKey = s.Controls.TeamChatKey;
            ItemSlot1 = s.Controls.Items.Slot1;
            ItemSlot2 = s.Controls.Items.Slot2;
            ItemSlot3 = s.Controls.Items.Slot3;
            ItemSlot4 = s.Controls.Items.Slot4;
            ItemSlot5 = s.Controls.Items.Slot5;
            ItemSlot6 = s.Controls.Items.Slot6;
        }
    }
}
