using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Threading;
using System.Windows.Forms;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class ThirdPersonCameraDebuff : DebuffBase
    {
        public override string Name => "Камера от третьего лица";

        private const int WheelNotches = 20;

        public override void Apply()
        {
            InputHookHost.Instance.SendKey((int)Keys.I);
            for (int i = 0; i < WheelNotches; i++)
            {
                InputHookHost.Instance.SendWheel(-120);
                Thread.Sleep(5);
            }
            InputHookHost.Instance.SetCameraWheelBlocked(true);
            InputHookHost.Instance.BlockKey((int)Keys.I);
            Console.WriteLine("[ThirdPerson] enabled");
        }
        public override void Remove()
        {
            InputHookHost.Instance.UnblockKey((int)Keys.I);
            InputHookHost.Instance.SetCameraWheelBlocked(false);
            InputHookHost.Instance.SendKey((int)Keys.I);
            Console.WriteLine("[ThirdPerson] disabled");
        }
    }
}
