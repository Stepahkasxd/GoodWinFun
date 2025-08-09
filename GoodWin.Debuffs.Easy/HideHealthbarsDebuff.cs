using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 60)]
    public class HideHealthbarsDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Скрыть полоски здоровья";

        private readonly int _applyButton;
        private readonly int _removeButton;

        public HideHealthbarsDebuff()
        {
            _applyButton = JoyCommandService.Instance.Register("dota_hud_healthbars 0");
            _removeButton = JoyCommandService.Instance.Register("dota_hud_healthbars 1");
        }

        public override void Apply()
        {
            JoyCommandService.Instance.Press(_applyButton);
            Console.WriteLine($"[HideHP] hidden for {Duration}s");
        }
        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
            Console.WriteLine("[HideHP] restored");
        }
    }
}
