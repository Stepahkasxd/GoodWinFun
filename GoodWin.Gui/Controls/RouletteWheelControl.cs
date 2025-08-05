using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GoodWin.Gui.Controls
{
    public class RouletteWheelControl : FrameworkElement
    {
        public ObservableCollection<Models.RouletteSegment> Segments
        {
            get => (ObservableCollection<Models.RouletteSegment>)GetValue(SegmentsProperty);
            set => SetValue(SegmentsProperty, value);
        }

        public static readonly DependencyProperty SegmentsProperty =
            DependencyProperty.Register(
                nameof(Segments),
                typeof(ObservableCollection<Models.RouletteSegment>),
                typeof(RouletteWheelControl),
                new PropertyMetadata(null, OnSegmentsChanged));

        public double WheelOpacity
        {
            get => (double)GetValue(WheelOpacityProperty);
            set => SetValue(WheelOpacityProperty, value);
        }

        public static readonly DependencyProperty WheelOpacityProperty =
            DependencyProperty.Register(nameof(WheelOpacity), typeof(double), typeof(RouletteWheelControl),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        private readonly RotateTransform _rotate = new RotateTransform(0);

        public RouletteWheelControl()
        {
            Segments = new ObservableCollection<Models.RouletteSegment>();
            RenderTransform = _rotate;
            RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private static void OnSegmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (RouletteWheelControl)d;
            if (e.OldValue is ObservableCollection<Models.RouletteSegment> oldCollection)
            {
                oldCollection.CollectionChanged -= control.SegmentsCollectionChanged;
                foreach (var seg in oldCollection)
                    seg.PropertyChanged -= control.SegmentPropertyChanged;
            }
            if (e.NewValue is ObservableCollection<Models.RouletteSegment> newCollection)
            {
                newCollection.CollectionChanged += control.SegmentsCollectionChanged;
                foreach (var seg in newCollection)
                    seg.PropertyChanged += control.SegmentPropertyChanged;
            }
            control.InvalidateVisual();
        }

        private void SegmentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (Models.RouletteSegment seg in e.OldItems)
                    seg.PropertyChanged -= SegmentPropertyChanged;
            if (e.NewItems != null)
                foreach (Models.RouletteSegment seg in e.NewItems)
                    seg.PropertyChanged += SegmentPropertyChanged;
            InvalidateVisual();
        }

        private void SegmentPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (Segments.Count == 0) return;

            double radius = Math.Min(ActualWidth, ActualHeight) / 2;
            var center = new Point(ActualWidth / 2, ActualHeight / 2);
            double angle = 360.0 / Segments.Count;
            for (int i = 0; i < Segments.Count; i++)
            {
                var seg = Segments[i];
                Brush brush = (Brush)new BrushConverter().ConvertFromString(seg.ColorHex);
                brush.Opacity = seg.Opacity * WheelOpacity;
                var geom = CreateSlice(center, radius, angle * i, angle);
                dc.DrawGeometry(brush, null, geom);
                if (!string.IsNullOrEmpty(seg.ImagePath) && System.IO.File.Exists(seg.ImagePath))
                {
                    var img = new ImageBrush(new System.Windows.Media.Imaging.BitmapImage(new Uri(seg.ImagePath, UriKind.Absolute)));
                    img.Opacity = seg.Opacity * WheelOpacity;
                    dc.DrawGeometry(img, null, geom);
                }

                if (!string.IsNullOrEmpty(seg.Label))
                {
                    double arcLength = Math.PI * radius * 0.6 * angle / 180.0;
                    var text = new FormattedText(
                        seg.Label,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        14,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip)
                    {
                        TextAlignment = TextAlignment.Center,
                        Trimming = TextTrimming.CharacterEllipsis,
                        MaxTextWidth = arcLength
                    };
                    var midAngle = (angle * i + angle / 2) * Math.PI / 180;
                    var textPoint = new Point(
                        center.X + radius * 0.6 * Math.Cos(midAngle),
                        center.Y + radius * 0.6 * Math.Sin(midAngle));
                    var fill = Brushes.Black.Clone();
                    var stroke = Brushes.White.Clone();
                    double opacity = seg.Opacity * WheelOpacity;
                    fill.Opacity = opacity;
                    stroke.Opacity = opacity;
                    var geometry = text.BuildGeometry(new Point(textPoint.X - text.Width / 2, textPoint.Y - text.Height / 2));
                    dc.DrawGeometry(fill, new Pen(stroke, 1), geometry);
                }
            }
        }

        private static Geometry CreateSlice(Point center, double radius, double startAngle, double sweepAngle)
        {
            double startRad = startAngle * Math.PI / 180;
            double endRad = (startAngle + sweepAngle) * Math.PI / 180;
            Point p1 = new Point(center.X + radius * Math.Cos(startRad), center.Y + radius * Math.Sin(startRad));
            Point p2 = new Point(center.X + radius * Math.Cos(endRad), center.Y + radius * Math.Sin(endRad));
            bool largeArc = sweepAngle > 180;
            var figure = new PathFigure { StartPoint = center };
            figure.Segments.Add(new LineSegment(p1, true));
            figure.Segments.Add(new ArcSegment(p2, new Size(radius, radius), sweepAngle, largeArc, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(center, true));
            var geom = new PathGeometry();
            geom.Figures.Add(figure);
            return geom;
        }

        public void Spin(int durationMs, int targetIndex, Action<Models.RouletteSegment> onCompleted)
        {
            if (Segments.Count == 0 || targetIndex < 0 || targetIndex >= Segments.Count)
                return;
            double angle = 360.0 / Segments.Count;
            double targetAngle = -(targetIndex * angle + angle / 2 + 360 * 3);
            var anim = new DoubleAnimation(targetAngle, TimeSpan.FromMilliseconds(durationMs))
            {
                AccelerationRatio = 0.1,
                DecelerationRatio = 0.9,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            anim.Completed += (s, e) => onCompleted?.Invoke(Segments[targetIndex]);
            _rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
        }
    }
}
