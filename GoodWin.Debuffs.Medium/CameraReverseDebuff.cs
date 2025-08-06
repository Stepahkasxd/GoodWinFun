using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 0, 999, 60)]
    public class CameraReverseDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Инверсия камеры";
        public override void Apply()
        {
            InputHookHost.Instance.Cmd("dota_camera_reverse 1");
            Console.WriteLine($"[CameraReverse] enabled for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.Cmd("dota_camera_reverse 0");
            Console.WriteLine("[CameraReverse] disabled");
        }
    }
}
