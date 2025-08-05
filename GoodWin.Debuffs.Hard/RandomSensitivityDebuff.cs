using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 30)]
    public class RandomSensitivityDebuff : DebuffBase
    {
        private CancellationTokenSource? _cts;
        private const int Duration = 30;
        public override string Name => "Случайная чувствительность";
        public override void Apply()
        {
            _cts = new CancellationTokenSource();
            var rnd = new Random();
            Task.Run(async () =>
            {
                var end = DateTime.UtcNow.AddSeconds(Duration);
                while (!_cts.Token.IsCancellationRequested && DateTime.UtcNow < end)
                {
                    var sens = rnd.NextDouble() * 3 + 0.5;
                    InputHookHost.Instance.Cmd($"sensitivity {sens:F2}");
                    await Task.Delay(1000);
                }
            }, _cts.Token);
            Console.WriteLine($"[RandomSens] running for {Duration}s");
        }
        public override void Remove()
        {
            _cts?.Cancel();
            Console.WriteLine("[RandomSens] restored");
        }
    }
}
