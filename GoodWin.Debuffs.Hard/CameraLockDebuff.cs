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

        private readonly int _applyButton;
        private readonly int _removeButton;

        public CameraLockDebuff()
        {
            _applyButton = JoyCommandService.Instance.Register("dota_camera_lock 1");
            _removeButton = JoyCommandService.Instance.Register("dota_camera_lock 0");
        }

        public override void Apply()
        {
            JoyCommandService.Instance.Press(_applyButton);
            InputHookHost.Instance.SetCameraWheelBlocked(true);
            Console.WriteLine($"[CameraLock] enabled for {Duration}s");
        }
        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
            InputHookHost.Instance.SetCameraWheelBlocked(false);
            Console.WriteLine("[CameraLock] disabled");
        }
    }
}
