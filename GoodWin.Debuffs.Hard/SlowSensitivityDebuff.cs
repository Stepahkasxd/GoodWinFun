using GoodWin.Core;
using System;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class SlowSensitivityDebuff : DebuffBase
    {
        private int _originalSpeed;
        private const int Duration = 60;
        public override string Name => "Медленная сенса";
        public override void Apply()
        {
            _originalSpeed = GetMouseSpeed();
            int speed = 1; // minimum
            SetMouseSpeed(speed);
            Console.WriteLine($"[SlowSens] speed set to {speed} for {Duration}s");
        }
        public override void Remove()
        {
            SetMouseSpeed(_originalSpeed);
            Console.WriteLine("[SlowSens] restored");
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
