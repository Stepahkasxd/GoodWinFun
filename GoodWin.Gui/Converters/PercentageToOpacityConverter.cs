using System;
using System.Globalization;
using System.Windows.Data;

namespace GoodWin.Gui.Converters
{
    public class PercentageToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
            {
                return i / 100.0;
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return (int)Math.Round(d * 100);
            }
            return 100;
        }
    }
}
