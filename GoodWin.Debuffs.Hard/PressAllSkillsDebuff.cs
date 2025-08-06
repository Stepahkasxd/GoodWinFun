using GoodWin.Core;
using GoodWin.Utils;
using System.Threading;
using System.Windows.Forms;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 1)]
    public class PressAllSkillsDebuff : DebuffBase, IInputDebuff
    {
        public override string Name => "Прожать все скилы";
        public override void Apply()
        {
            var keys = new[] { Keys.Q, Keys.W, Keys.E, Keys.R, Keys.D, Keys.F };
            foreach (var k in keys)
            {
                InputHookHost.Instance.SendKey((int)k);
                Thread.Sleep(50);
            }
        }
        public override void Remove() { }
    }
}
