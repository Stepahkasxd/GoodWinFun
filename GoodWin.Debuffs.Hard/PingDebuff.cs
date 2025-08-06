using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class PingDebuff : DebuffBase
    {
        private readonly int _ping;
        private const int Duration = 60;
        public override string Name => "Высокий пинг";
        public PingDebuff()
        {
            var arr = new[] { 200, 300, 400 };
            _ping = arr[new Random().Next(arr.Length)];
        }
        public override void Apply()
        {
            InputHookHost.Instance.Cmd($"net_fakelag {_ping}");
            Console.WriteLine($"[Ping] fake lag {_ping}ms for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.Cmd("net_fakelag 0");
            Console.WriteLine("[Ping] restored");
        }
    }
}
