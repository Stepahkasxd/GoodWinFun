using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Windows;
using System.Windows.Media;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 60)]
    public class HideMinimapDebuff : DebuffBase, IOverlayDebuff
    {
        private Guid _overlayId;
        public override string Name => "Скрыть миникарту";
        public override void Apply()
        {
            double size = 256;
            double h = SystemParameters.PrimaryScreenHeight;
            _overlayId = OverlayWindow.Instance.AddOverlay(dc =>
            {
                var brush = Brushes.Black;
                dc.DrawRectangle(brush, null, new Rect(0, h - size, size, size));
            });
            Console.WriteLine("[HideMinimap] applied");
        }
        public override void Remove()
        {
            OverlayWindow.Instance.RemoveOverlay(_overlayId);
            Console.WriteLine("[HideMinimap] removed");
        }
    }
}
