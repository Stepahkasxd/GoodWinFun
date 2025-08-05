using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GoodWin.Gui.Converters
{
    public class SegmentToGeometryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] is int index &&
                values[1] is int total &&
                total > 0)
            {
                double anglePerSegment = 360.0 / total;
                double startAngle = index * anglePerSegment;
                double endAngle = startAngle + anglePerSegment;
                double radius = 150; // preview radius
                Point center = new(radius, radius);

                double startRadians = startAngle * Math.PI / 180.0;
                double endRadians = endAngle * Math.PI / 180.0;

                Point startPoint = new(
                    center.X + radius * Math.Cos(startRadians),
                    center.Y + radius * Math.Sin(startRadians));

                Point endPoint = new(
                    center.X + radius * Math.Cos(endRadians),
                    center.Y + radius * Math.Sin(endRadians));

                bool isLargeArc = anglePerSegment > 180.0;

                var figure = new PathFigure { StartPoint = center };
                figure.Segments.Add(new LineSegment(startPoint, true));
                figure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), anglePerSegment, isLargeArc, SweepDirection.Clockwise, true));
                figure.Segments.Add(new LineSegment(center, true));

                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                return geometry;
            }
            return Geometry.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
