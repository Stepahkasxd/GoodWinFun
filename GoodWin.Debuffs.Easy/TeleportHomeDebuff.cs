using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 1)]
    public class TeleportHomeDebuff : DebuffBase
    {
        public override string Name => "ТП домой";

        private readonly int _button;

        public TeleportHomeDebuff()
        {
            _button = JoyCommandService.Instance.Register("dota_item_use item_tpscroll");
        }

        public override void Apply()
        {
            JoyCommandService.Instance.Press(_button);
            Console.WriteLine("[TpHome] teleport home used");
        }
        public override void Remove() { }
    }
}
