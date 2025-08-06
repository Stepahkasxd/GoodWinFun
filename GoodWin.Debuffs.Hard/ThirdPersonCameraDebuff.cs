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
        public override void Apply()
        {
            InputHookHost.Instance.SendKey((int)Keys.I);
            InputHookHost.Instance.Cmd("dota_camera_distance 2000");
            InputHookHost.Instance.BlockKey((int)Keys.I);
            Console.WriteLine("[ThirdPerson] enabled");
        }
        public override void Remove()
        {
            InputHookHost.Instance.UnblockKey((int)Keys.I);
            InputHookHost.Instance.Cmd("dota_camera_distance 1134");
            InputHookHost.Instance.SendKey((int)Keys.I);
            Console.WriteLine("[ThirdPerson] disabled");
        }
    }
}
