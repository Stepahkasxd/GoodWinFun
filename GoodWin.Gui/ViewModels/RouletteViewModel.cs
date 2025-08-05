using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodWin.Core;
using GoodWin.Gui.Models;
using GoodWin.Gui.Services;

namespace GoodWin.Gui.ViewModels
{
    public partial class RouletteViewModel : ObservableObject
    {
        public ObservableCollection<RouletteSegment> Segments { get; } = new();

        [ObservableProperty]
        private int wheelOpacity = 100;

        [ObservableProperty]
        private int spinDuration = 3000; // milliseconds

        [ObservableProperty]
        private int segmentCount = 0;

        private readonly UserSettingsService _service;

        private static readonly string[] DefaultColors = new[]
        {
            "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#00FFFF", "#FF00FF",
            "#800000", "#008000", "#000080", "#808000", "#008080", "#800080"
        };

        public IRelayCommand SaveCommand { get; }

        public RouletteViewModel()
        {
            _service = new UserSettingsService("usersettings.json");
            LoadFromSettings();
            SaveCommand = new RelayCommand(Save);
        }

        private void LoadFromSettings()
        {
            var rs = _service.Settings.Roulette;
            WheelOpacity = (int)(rs.WheelOpacity * 100);
            SpinDuration = rs.SpinDuration;
            foreach (var seg in rs.Segments)
            {
                Segments.Add(new RouletteSegment
                {
                    ColorHex = seg.ColorHex,
                    Opacity = seg.Opacity,
                    ImagePath = seg.ImagePath,
                    Label = seg.Label
                });
            }

            SegmentCount = Segments.Count;
        }

        private void Save()
        {
            var rs = _service.Settings.Roulette;
            rs.WheelOpacity = WheelOpacity / 100.0;
            rs.SpinDuration = SpinDuration;
            rs.Segments = Segments.Select(s => new RouletteSegmentSettings
            {
                ColorHex = s.ColorHex,
                Opacity = s.Opacity,
                ImagePath = s.ImagePath,
                Label = s.Label
            }).ToList();
            _service.Save();
        }

        partial void OnSegmentCountChanged(int value)
        {
            if (value < 1)
            {
                SegmentCount = 1;
                return;
            }
            if (value > 12)
            {
                SegmentCount = 12;
                return;
            }

            while (Segments.Count < value)
            {
                var color = DefaultColors[Segments.Count % DefaultColors.Length];
                Segments.Add(new RouletteSegment
                {
                    ColorHex = color,
                    Opacity = 1.0
                });
            }
            while (Segments.Count > value)
            {
                Segments.RemoveAt(Segments.Count - 1);
            }
        }

        partial void OnSpinDurationChanged(int value)
        {
            if (value > 10000)
            {
                SpinDuration = 10000;
            }
            else if (value < 0)
            {
                SpinDuration = 0;
            }
        }
    }
}
