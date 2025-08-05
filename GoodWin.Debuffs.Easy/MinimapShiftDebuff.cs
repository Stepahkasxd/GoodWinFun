using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 60)]
    public class MinimapShiftDebuff : DebuffBase
    {
        private const int Duration = 60;
        private const int EnableKey = 0x36; // 6
        private const int DisableKey = 0x37; // 7
        public override string Name => "Сдвиг миникарты";
        public override void Apply()
        {
            InputHookHost.Instance.SendKey(EnableKey);
            Console.WriteLine($"[MinimapShift] shifted for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.SendKey(DisableKey);
            Console.WriteLine("[MinimapShift] restored");
        }
    }
}
