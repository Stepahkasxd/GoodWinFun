using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 1)]
    public class TeleportHomeDebuff : DebuffBase
    {
        public override string Name => "ТП домой";
        public override void Apply()
        {
            InputHookHost.Instance.Cmd("dota_item_use item_tpscroll");
            Console.WriteLine("[TpHome] teleport home used");
        }
        public override void Remove() { }
    }
}
