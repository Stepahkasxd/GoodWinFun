using System;
using System.Collections.Generic;

namespace GoodWin.Core
{
    public class UserSettings
    {
        public Dota2Settings Dota2 { get; set; } = new Dota2Settings();
        public ControlsSettings Controls { get; set; } = new ControlsSettings();
        public RouletteSettings Roulette { get; set; } = new RouletteSettings();
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

    public class RouletteSettings
    {
        public PhaseSettings Easy { get; set; } = new PhaseSettings { MinSpins = 4, MaxSpins = 10 };
        public PhaseSettings Medium { get; set; } = new PhaseSettings { MinSpins = 10, MaxSpins = 15 };
        public double HardChancePerMinute { get; set; } = 12;

        public double WheelOpacity { get; set; } = 1.0;

        private int _spinDuration = 3000;
        public int SpinDuration
        {
            get => _spinDuration;
            set => _spinDuration = Math.Clamp(value, 0, 10000);
        }
        public double WindowLeft { get; set; } = 0;
        public double WindowTop { get; set; } = 0;
        public double WindowWidth { get; set; } = 300;
        public double WindowHeight { get; set; } = 300;
        public List<RouletteSegmentSettings> Segments { get; set; } = new();
    }

    public class RouletteSegmentSettings
    {
        public string ColorHex { get; set; } = "#FF0000";
        public double Opacity { get; set; } = 1.0;
        public string? ImagePath { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class PhaseSettings
    {
        public int MinSpins { get; set; }
        public int MaxSpins { get; set; }
    }
}

