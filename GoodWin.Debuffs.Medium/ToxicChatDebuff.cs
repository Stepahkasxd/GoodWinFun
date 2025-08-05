using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 10, 15, 60)]
    public class ToxicChatDebuff : DebuffBase
    {
        public override string Name => "Токсичный чат";

        private readonly string[] _lines = new[]
        {
            "You're trash!",
            "Report this noob",
            "Go feeders!",
            "Learn to play!",
        };

        private CancellationTokenSource? _cts;

        public override void Apply()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _ = Task.Run(async () =>
            {
                var rnd = new Random();
                var end = DateTime.UtcNow.AddSeconds(60);
                while (!token.IsCancellationRequested && DateTime.UtcNow < end)
                {
                    InputHookHost.Instance.SendKey((int)Keys.Enter);
                    await Task.Delay(10, token);
                    InputHookHost.Instance.SendKey((int)Keys.Tab);
                    await Task.Delay(10, token);

                    var msg = _lines[rnd.Next(_lines.Length)];
                    InputHookHost.Instance.SendText(msg);
                    InputHookHost.Instance.SendKey((int)Keys.Enter);

                    await Task.Delay(1000, token);
                }
            }, token);

            Console.WriteLine("[ToxicChat] Запущен на 60 сек.");
        }

        public override void Remove()
        {
            _cts?.Cancel();
            Console.WriteLine("[ToxicChat] Дебафф завершён");
        }
    }
}
