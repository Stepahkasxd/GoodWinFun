using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 0, 999, 60)]
    public class Fps12Debuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "FPS 12";
        public override void Apply()
        {
            InputHookHost.Instance.Cmd("fps_max 12");
            Console.WriteLine($"[FPS12] limited for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.Cmd("fps_max 120");
            Console.WriteLine("[FPS12] restored");
        }
    }
}
