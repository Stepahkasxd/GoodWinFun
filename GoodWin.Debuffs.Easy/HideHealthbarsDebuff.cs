using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 60)]
    public class HideHealthbarsDebuff : DebuffBase
    {
        private const int Duration = 60;
        private const int EnableKey = 0x91; // ScrollLock
        private const int DisableKey = 0x13; // Pause
        public override string Name => "Скрыть полоски здоровья";
        public override void Apply()
        {
            InputHookHost.Instance.SendKey(EnableKey);
            Console.WriteLine($"[HideHP] hidden for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.SendKey(DisableKey);
            Console.WriteLine("[HideHP] restored");
        }
    }
}
