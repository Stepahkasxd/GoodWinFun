using GoodWin.Core;
using WindowsInput;
using WindowsInput.Native;

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 15, 30, 1)]
    public class AutoSkillDebuff : DebuffBase, IInputDebuff
    {
        private readonly InputSimulator _sim = new InputSimulator();
        private readonly string _slot1, _slot3;
        public override string Name => "ПЕРВЫЙ СКИЛЛ И ТРЕТИЙ";
        public AutoSkillDebuff(string slot1, string slot3)
        {
            _slot1 = slot1; _slot3 = slot3;
        }
        public override void Apply()
        {
            if (Enum.TryParse("VK_" + _slot1, out VirtualKeyCode key1))
                _sim.Keyboard.KeyPress(key1);
            if (Enum.TryParse("VK_" + _slot3, out VirtualKeyCode key3))
                _sim.Keyboard.KeyPress(key3);
        }
        public override void Remove() { }
    }
}