using System;
using GoodWin.Core;
using GoodWin.Utils;

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 10, 15, 45)]
    public class MouseLagDebuff : DebuffBase
    {
        public override string Name => "Лаг мыши";

        public override void Apply()
        {
            InputHookHost.Instance.SetMouseLag(true);
            Console.WriteLine($"[MouseLag] Лаг мыши включён, вернётся через 45 сек.");
        }

        public override void Remove()
        {
            InputHookHost.Instance.SetMouseLag(false);
            Console.WriteLine("[MouseLag] Лаг мыши отключён");
        }
    }
}
