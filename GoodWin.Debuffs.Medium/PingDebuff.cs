using System;
using GoodWin.Core;
using GoodWin.Utils;
using GoodWin.Debuffs.Hard; 

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 5, 10, 20)]
    public class PingDebuff : DebuffBase
    {
        public override string Name => "Пинг-лаг";

        public override void Apply()
        {
            // Запускаем InputLagDebuff параллельно
            var lag = new InputLagDebuff();
            lag.Apply();
        }

        public override void Remove()
        {
            // Останавливаем его
            var lag = new InputLagDebuff();
            lag.Remove();
        }
    }
}
