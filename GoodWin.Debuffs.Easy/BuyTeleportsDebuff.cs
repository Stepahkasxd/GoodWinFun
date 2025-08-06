using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 1)]
    public class BuyTeleportsDebuff : DebuffBase
    {
        public override string Name => "Купить 5 ТП";
        public override void Apply()
        {
            for (int i = 0; i < 5; i++)
                InputHookHost.Instance.Cmd("dota_item_purchase item_tpscroll");
            Console.WriteLine("[BuyTP] purchased 5 TP");
        }
        public override void Remove() { }
    }
}
