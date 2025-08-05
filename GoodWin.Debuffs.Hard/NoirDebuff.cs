using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Windows;
using System.Windows.Media;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 30)]
    public class NoirDebuff : DebuffBase
    {
        private const int Duration = 30;
        public override string Name => "Нуар-фильтр";
        public override void Apply()
        {
            OverlayWindow.Instance.AddOverlay(dc =>
            {
                var rect = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)), null, rect);
            });
            Console.WriteLine($"[Noir] applied for {Duration}s");
        }
        public override void Remove()
        {
            OverlayWindow.Instance.ClearOverlays();
            Console.WriteLine("[Noir] removed");
        }
    }
}