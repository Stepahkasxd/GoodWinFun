using System.ComponentModel;
using System.Runtime.CompilerServices;
using GoodWin.Core;

namespace GoodWin.Gui.Models
{
    public class RouletteSegment : INotifyPropertyChanged
    {
        private string _colorHex = "#FF0000";
        public string ColorHex
        {
            get => _colorHex;
            set { if (_colorHex != value) { _colorHex = value; OnPropertyChanged(); } }
        }

        private double _opacity = 1.0;
        public double Opacity
        {
            get => _opacity;
            set { if (_opacity != value) { _opacity = value; OnPropertyChanged(); } }
        }

        private string? _imagePath;
        public string? ImagePath
        {
            get => _imagePath;
            set { if (_imagePath != value) { _imagePath = value; OnPropertyChanged(); } }
        }

        private string _label = string.Empty;
        public string Label
        {
            get => _label;
            set { if (_label != value) { _label = value; OnPropertyChanged(); } }
        }

        public IDebuff? Debuff { get; set; }
        public Event? AssociatedEvent { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
