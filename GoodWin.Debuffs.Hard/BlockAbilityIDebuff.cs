using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Windows.Forms;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class BlockAbilityIDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Блок I";
        public override void Apply()
        {
            InputHookHost.Instance.BlockKey((int)Keys.I);
            Console.WriteLine($"[BlockI] blocked for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.UnblockKey((int)Keys.I);
            Console.WriteLine("[BlockI] unblocked");
        }
    }
}
