using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 0, 999, 60)]
    public class ViewportScaleDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Ужасное качество";
        public override void Apply()
        {
            InputHookHost.Instance.Cmd("mat_viewportscale 0.1");
            Console.WriteLine($"[ViewportScale] 0.1 for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.Cmd("mat_viewportscale 1");
            Console.WriteLine("[ViewportScale] restored");
        }
    }
}
