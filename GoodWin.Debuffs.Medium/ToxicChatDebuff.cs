using GoodWin.Core;
using GoodWin.Utils;
using System;
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
            "Learn to play!"
        };

        public override void Apply()
        {
            _ = Task.Run(async () =>
            {
                var rnd = new Random();
                var end = DateTime.UtcNow.AddSeconds(60);
                while (DateTime.UtcNow < end)
                {
                    // открыть чат
                    InputHookHost.Instance.SendKey((int)Keys.Enter);
                    await Task.Delay(50);
                    // общий чат
                    InputHookHost.Instance.SendKey((int)Keys.Tab);
                    await Task.Delay(50);

                    // написать сообщение
                    var msg = _lines[rnd.Next(_lines.Length)];
                    InputHookHost.Instance.SendText(msg);
                    InputHookHost.Instance.SendKey((int)Keys.Enter);

                    await Task.Delay(5000);
                }
            });

            Console.WriteLine("[ToxicChat] Запущен на 60 сек.");
        }

        public override void Remove()
        {
            Console.WriteLine("[ToxicChat] Дебафф завершён");
        }
    }
}