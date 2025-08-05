using System;
using GoodWin.Core;
using GoodWin.Utils;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 10, 15, 30)]
    public class HideHUDDebuff : DebuffBase
    {
        public override string Name => "Скрыть HUD";

        private const int EnableKey = 0xDB; // [
        private const int DisableKey = 0xDD; // ]

        public override void Apply()
        {
            InputHookHost.Instance.SendKey(EnableKey);
            Console.WriteLine("[HideHUD] HUD скрыт");
        }

        public override void Remove()
        {
            InputHookHost.Instance.SendKey(DisableKey);
            Console.WriteLine("[HideHUD] HUD возвращён");
        }
    }
}
