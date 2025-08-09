using GoodWin.Core;
using GoodWin.Keybinds;
using GoodWin.Utils;
using System.Threading;
using System.Windows.Forms;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 1)]
    public class PressAllItemsDebuff : DebuffBase, IInputDebuff
    {
        private readonly IKeybindService _keybinds;
        public PressAllItemsDebuff(IKeybindService keybinds) => _keybinds = keybinds;

        public override string Name => "Прожать все предметы";
        public override void Apply()
        {
            var labels = new[] { "Inventory1", "Inventory2", "Inventory3", "Inventory4", "Inventory5", "Inventory6" };
            foreach (var l in labels)
            {
                if (_keybinds.Bindings.TryGetValue(l, out var s) && Enum.TryParse<Keys>(s, true, out var k))
                {
                    InputHookHost.Instance.SendKey((int)k);
                    Thread.Sleep(50);
                }
            }
        }
        public override void Remove() { }
    }
}
