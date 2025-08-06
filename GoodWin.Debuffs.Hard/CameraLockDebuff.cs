using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class CameraLockDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Блокировка камеры";
        public override void Apply()
        {
            InputHookHost.Instance.Cmd("dota_camera_lock 1");
            InputHookHost.Instance.SetCameraWheelBlocked(true);
            Console.WriteLine($"[CameraLock] enabled for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.Cmd("dota_camera_lock 0");
            InputHookHost.Instance.SetCameraWheelBlocked(false);
            Console.WriteLine("[CameraLock] disabled");
        }
    }
}
