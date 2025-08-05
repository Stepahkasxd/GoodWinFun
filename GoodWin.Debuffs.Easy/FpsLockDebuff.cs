using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 60)]
    public class FpsLockDebuff : DebuffBase
    {
        private const int Duration = 60;
        private const int EnableKey = 0x34; // 4
        private const int DisableKey = 0x35; // 5
        public override string Name => "Лимит FPS";
        public override void Apply()
        {
            InputHookHost.Instance.SendKey(EnableKey);
            Console.WriteLine($"[FpsLock] FPS locked at 30 for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.SendKey(DisableKey);
            Console.WriteLine("[FpsLock] FPS unlocked to 120");
        }
    }
}