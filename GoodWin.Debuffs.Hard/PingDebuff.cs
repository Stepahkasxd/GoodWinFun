using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Collections.Generic;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class PingDebuff : DebuffBase
    {
        private readonly int _ping;
        private const int Duration = 60;
        public override string Name => "Высокий пинг";

        private readonly Dictionary<int, int> _buttons = new();
        private readonly int _removeButton;

        public PingDebuff()
        {
            var arr = new[] { 200, 300, 400 };
            _ping = arr[new Random().Next(arr.Length)];
            foreach (var v in arr)
                _buttons[v] = JoyCommandService.Instance.Register($"net_fakelag {v}");
            _removeButton = JoyCommandService.Instance.Register("net_fakelag 0");
        }
        public override void Apply()
        {
            JoyCommandService.Instance.Press(_buttons[_ping]);
            Console.WriteLine($"[Ping] fake lag {_ping}ms for {Duration}s");
        }
        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
            Console.WriteLine("[Ping] restored");
        }
    }
}
