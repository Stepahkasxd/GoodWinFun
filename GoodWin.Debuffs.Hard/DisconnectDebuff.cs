using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 1)]
    public class DisconnectDebuff : DebuffBase
    {
        public override string Name => "Дисконнект";

        private readonly int _button;

        public DisconnectDebuff()
        {
            _button = JoyCommandService.Instance.Register("disconnect");
        }

        public override void Apply()
        {
            JoyCommandService.Instance.Press(_button);
            Console.WriteLine("[Disconnect] command sent");
        }
        public override void Remove() { }
    }
}
