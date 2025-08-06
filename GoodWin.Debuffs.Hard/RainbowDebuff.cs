using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Windows;
using System.Windows.Media;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class RainbowDebuff : DebuffBase, IOverlayDebuff
    {
        private Guid _overlayId;
        public override string Name => "My Little Pony";
        public override void Apply()
        {
            _overlayId = OverlayWindow.Instance.AddOverlay(dc =>
            {
                var rect = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
                var brush = new LinearGradientBrush();
                brush.StartPoint = new Point(0, 0);
                brush.EndPoint = new Point(1, 0);
                brush.GradientStops.Add(new GradientStop(Colors.Red, 0));
                brush.GradientStops.Add(new GradientStop(Colors.Orange, 0.17));
                brush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.33));
                brush.GradientStops.Add(new GradientStop(Colors.Green, 0.5));
                brush.GradientStops.Add(new GradientStop(Colors.Blue, 0.67));
                brush.GradientStops.Add(new GradientStop(Colors.Indigo, 0.83));
                brush.GradientStops.Add(new GradientStop(Colors.Violet, 1));
                brush.Opacity = 0.5;
                dc.DrawRectangle(brush, null, rect);
            });
            Console.WriteLine("[Rainbow] applied");
        }
        public override void Remove()
        {
            OverlayWindow.Instance.RemoveOverlay(_overlayId);
            Console.WriteLine("[Rainbow] removed");
        }
    }
}
