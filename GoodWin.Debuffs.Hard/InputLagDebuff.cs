using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class InputLagDebuff : DebuffBase
    {
        private const int Duration = 60;
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