using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 1)]
    public class DisconnectDebuff : DebuffBase
    {
        public override string Name => "Дисконнект";
        public override void Apply()
        {
            InputHookHost.Instance.Cmd("disconnect");
            Console.WriteLine("[Disconnect] command sent");
        }
        public override void Remove() { }
    }
}
