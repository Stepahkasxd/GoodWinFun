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

        private readonly int _applyButton;
        private readonly int _removeButton;

        public CameraReverseDebuff()
        {
            _applyButton = JoyCommandService.Instance.Register("dota_camera_reverse 1");
            _removeButton = JoyCommandService.Instance.Register("dota_camera_reverse 0");
        }
        public override void Apply()
        {
            JoyCommandService.Instance.Press(_applyButton);
            Console.WriteLine($"[CameraReverse] enabled for {Duration}s");
        }
        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
            Console.WriteLine("[CameraReverse] disabled");
        }
    }
}
