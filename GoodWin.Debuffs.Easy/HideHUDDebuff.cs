using System;
using GoodWin.Core;
using GoodWin.Utils;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 10, 15, 60)]
    public class HideHUDDebuff : DebuffBase
    {
        public override string Name => "Скрыть HUD";

        private readonly int _applyButton;
        private readonly int _removeButton;

        public HideHUDDebuff()
        {
            _applyButton = JoyCommandService.Instance.Register("dota_hud_hide_mainhud 1");
            _removeButton = JoyCommandService.Instance.Register("dota_hud_hide_mainhud 0");
        }

        public override void Apply()
        {
            JoyCommandService.Instance.Press(_applyButton);
            Console.WriteLine("[HideHUD] HUD скрыт");
        }

        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
            Console.WriteLine("[HideHUD] HUD возвращён");
        }
    }
}
