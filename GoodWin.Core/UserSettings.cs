namespace GoodWin.Core
{
    public class UserSettings
    {
        public Dota2Settings Dota2 { get; set; } = new Dota2Settings();
        public ControlsSettings Controls { get; set; } = new ControlsSettings();
    }

    public class Dota2Settings
    {
        public string Path { get; set; } = string.Empty;
    }

    public class ControlsSettings
    {
        public string ConsoleKey { get; set; } = "Oem3";
        public AbilityKeys Abilities { get; set; } = new AbilityKeys();
        public ItemKeys Items { get; set; } = new ItemKeys();
        public string ChatKey { get; set; } = "Return";
        public string TeamChatKey { get; set; } = "Y";
    }

    public class AbilityKeys
    {
        public string Slot1 { get; set; } = "Q";
        public string Slot2 { get; set; } = "W";
        public string Slot3 { get; set; } = "E";
        public string Ultimate { get; set; } = "R";
    }

    public class ItemKeys
    {
        public string Slot1 { get; set; } = "Z";
        public string Slot2 { get; set; } = "X";
        public string Slot3 { get; set; } = "C";
        public string Slot4 { get; set; } = "V";
        public string Slot5 { get; set; } = "B";
        public string Slot6 { get; set; } = "N";
    }
}

