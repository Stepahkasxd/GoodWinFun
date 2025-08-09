using GoodWin.Core;
using WindowsInput;

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 15, 30, 60)]
    public class AutoSkillDebuff : DebuffBase, IInputDebuff
    {
        private readonly InputSimulator _sim = new InputSimulator();
        private readonly VirtualKeyCode? _key1;
        private readonly VirtualKeyCode? _key3;
        public override string Name => "ПЕРВЫЙ СКИЛЛ И ТРЕТИЙ";
        public AutoSkillDebuff(string slot1, string slot3)
        {
            if (Enum.TryParse("VK_" + slot1, out VirtualKeyCode key1))
                _key1 = key1;
            if (Enum.TryParse("VK_" + slot3, out VirtualKeyCode key3))
                _key3 = key3;
        }
        public override void Apply()
        {
            if (_key1.HasValue)
                _sim.Keyboard.KeyPress(_key1.Value);
            if (_key3.HasValue)
                _sim.Keyboard.KeyPress(_key3.Value);
        }
        public override void Remove() { }
    }
}