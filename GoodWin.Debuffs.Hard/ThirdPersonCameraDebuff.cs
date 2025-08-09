using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Windows.Forms;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class ThirdPersonCameraDebuff : DebuffBase
    {
        public override string Name => "Камера от третьего лица";

        private readonly int _applyButton;
        private readonly int _removeButton;

        public ThirdPersonCameraDebuff()
        {
            _applyButton = JoyCommandService.Instance.Register("dota_camera_distance 2000");
            _removeButton = JoyCommandService.Instance.Register("dota_camera_distance 1134");
        }

        public override void Apply()
        {
            InputHookHost.Instance.SendKey((int)Keys.I);
            JoyCommandService.Instance.Press(_applyButton);
            InputHookHost.Instance.BlockKey((int)Keys.I);
            Console.WriteLine("[ThirdPerson] enabled");
        }
        public override void Remove()
        {
            InputHookHost.Instance.UnblockKey((int)Keys.I);
            JoyCommandService.Instance.Press(_removeButton);
            InputHookHost.Instance.SendKey((int)Keys.I);
            Console.WriteLine("[ThirdPerson] disabled");
        }
    }
}
