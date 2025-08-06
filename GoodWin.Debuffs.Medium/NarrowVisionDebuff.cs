using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Windows;
using System.Windows.Media;

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 10, 15, 60)]
    public class NarrowVisionDebuff : DebuffBase, IOverlayDebuff
    {
        private Guid _overlayId;
        public override string Name => "Сузить обзор";
        public override void Apply()
        {
            _overlayId = OverlayWindow.Instance.AddOverlay(dc =>
            {
                double w = SystemParameters.PrimaryScreenWidth;
                double h = SystemParameters.PrimaryScreenHeight;
                double marginX = w * 0.2;
                double marginY = h * 0.2;
                var brush = Brushes.Black;
                dc.DrawRectangle(brush, null, new Rect(0, 0, w, marginY));
                dc.DrawRectangle(brush, null, new Rect(0, h - marginY, w, marginY));
                dc.DrawRectangle(brush, null, new Rect(0, marginY, marginX, h - 2 * marginY));
                dc.DrawRectangle(brush, null, new Rect(w - marginX, marginY, marginX, h - 2 * marginY));
            });
            Console.WriteLine("[NarrowVision] applied");
        }
        public override void Remove()
        {
            OverlayWindow.Instance.RemoveOverlay(_overlayId);
            Console.WriteLine("[NarrowVision] removed");
        }
    }
}
