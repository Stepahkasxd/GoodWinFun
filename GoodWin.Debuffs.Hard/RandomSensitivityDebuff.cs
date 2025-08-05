using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class RandomSensitivityDebuff : DebuffBase
    {
        private CancellationTokenSource? _cts;
        private const int Duration = 60;
        private int _originalSpeed;
        public override string Name => "Случайная чувствительность";
        public override void Apply()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _originalSpeed = GetMouseSpeed();
            var rnd = new Random();
            Task.Run(async () =>
            {
                var end = DateTime.UtcNow.AddSeconds(Duration);
                while (!token.IsCancellationRequested && DateTime.UtcNow < end)
                {
                    int speed = rnd.Next(1, 21);
                    SetMouseSpeed(speed);
                    await Task.Delay(1000, token);
                }
            }, token);
            Console.WriteLine($"[RandomSens] running for {Duration}s");
        }
        public override void Remove()
        {
            _cts?.Cancel();
            SetMouseSpeed(_originalSpeed);
            Console.WriteLine("[RandomSens] restored");
        }

        private const int SPI_GETMOUSESPEED = 0x0070;
        private const int SPI_SETMOUSESPEED = 0x0071;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int fuWinIni);

        private static int GetMouseSpeed()
        {
            int speed = 0;
            SystemParametersInfo(SPI_GETMOUSESPEED, 0, ref speed, 0);
            return speed;
        }

        private static void SetMouseSpeed(int speed)
        {
            SystemParametersInfo(SPI_SETMOUSESPEED, 0, ref speed, 0);
        }
    }
}
