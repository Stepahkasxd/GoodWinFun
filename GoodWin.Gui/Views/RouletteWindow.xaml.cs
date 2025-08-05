using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GoodWin.Core;
using GoodWin.Gui.Services;
using GoodWin.Gui.ViewModels;

namespace GoodWin.Gui.Views
{
    public partial class RouletteWindow : Window
    {
        private readonly UserSettingsService _service;

        public RouletteWindow()
        {
            InitializeComponent();
            _service = new UserSettingsService("usersettings.json");
            var rs = _service.Settings.Roulette;
            Left = rs.WindowLeft;
            Top = rs.WindowTop;
            Width = rs.WindowWidth;
            Height = rs.WindowHeight;
            Closing += RouletteWindow_Closing;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void SavePosition_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RouletteViewModel vm)
            {
                var rs = _service.Settings.Roulette;
                rs.WindowLeft = Left;
                rs.WindowTop = Top;
                rs.WindowWidth = Width;
                rs.WindowHeight = Height;
                rs.WheelOpacity = vm.WheelOpacity / 100.0;
                rs.SpinDuration = vm.SpinDuration;
                rs.Segments = vm.Segments.Select(s => new RouletteSegmentSettings
                {
                    ColorHex = s.ColorHex,
                    Opacity = s.Opacity,
                    ImagePath = s.ImagePath,
                    Label = s.Label
                }).ToList();
                _service.Save();
            }
        }

        private void RouletteWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (DataContext is RouletteViewModel vm)
            {
                var rs = _service.Settings.Roulette;
                rs.WheelOpacity = vm.WheelOpacity / 100.0;
                rs.SpinDuration = vm.SpinDuration;
                rs.Segments = vm.Segments.Select(s => new RouletteSegmentSettings
                {
                    ColorHex = s.ColorHex,
                    Opacity = s.Opacity,
                    ImagePath = s.ImagePath,
                    Label = s.Label
                }).ToList();
                _service.Save();
            }
        }

        private void CloseEditor_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
