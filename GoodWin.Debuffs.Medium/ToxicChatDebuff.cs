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

        private readonly string[] _teamLines = new[]
        {
            "Оу супергерой опять слил, как будто у тя не руки а мать шлюха",
            "Спасибо что фидишь я бы без тебя не справился",
            "Еблан",
            "Уебмще, зачем тебе радик",
        };

        private readonly string[] _allLines = new[]
        {
            "Вы такие предсказуемые, что даже камень бы вас обыграл",
            "Снесите уже, я не могу играть с ними",
            "GG уроды, вы это проебали",
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
                    bool team = rnd.Next(2) == 0;
                    InputHookHost.Instance.SendKey((int)Keys.Enter);
                    await Task.Delay(10, token);
                    if (team)
                    {
                        InputHookHost.Instance.SendKey((int)Keys.Tab);
                        await Task.Delay(10, token);
                    }

                    var msg = team
                        ? _teamLines[rnd.Next(_teamLines.Length)]
                        : _allLines[rnd.Next(_allLines.Length)];
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
