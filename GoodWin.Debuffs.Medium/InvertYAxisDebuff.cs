using System;
using GoodWin.Core;
using GoodWin.Utils;

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 10, 15, 30)]
    public class InvertYAxisDebuff : DebuffBase
    {
        public override string Name => "Инвертировать ось Y";

        public override void Apply()
        {
            InputHookHost.Instance.SetInvertY(true);
            Console.WriteLine($"[InvertY] Ось Y инвертирована, вернётся через 30 сек.");
        }

        public override void Remove()
        {
            InputHookHost.Instance.SetInvertY(false);
            Console.WriteLine("[InvertY] Ось Y возвращена");
        }
    }
}