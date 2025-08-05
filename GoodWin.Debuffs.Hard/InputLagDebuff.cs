using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 45)]
    public class InputLagDebuff : DebuffBase
    {
        private const int Duration = 45;
        public override string Name => "Лаг ввода";
        public override void Apply()
        {
            InputHookHost.Instance.SetInputLag(true);
            Console.WriteLine($"[InputLag] enabled for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.SetInputLag(false);
            Console.WriteLine("[InputLag] disabled");
        }
    }
}