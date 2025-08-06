using GoodWin.Core;
using GoodWin.Utils;
using System.Windows.Forms;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 1)]
    public class PressAllItemsDebuff : DebuffBase, IInputDebuff
    {
        public override string Name => "Прожать все предметы";
        public override void Apply()
        {
            var keys = new[] { Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N };
            foreach (var k in keys)
                InputHookHost.Instance.SendKey((int)k);
        }
        public override void Remove() { }
    }
}
