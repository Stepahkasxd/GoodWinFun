using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 60)]
    public class MinimapShiftDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Сдвиг миникарты";

        private readonly int _applyButton;
        private readonly int _removeButton;

        public MinimapShiftDebuff()
        {
            _applyButton = JoyCommandService.Instance.Register("dota_minimap_position_option 0");
            _removeButton = JoyCommandService.Instance.Register("dota_minimap_position_option 1");
        }

        public override void Apply()
        {
            JoyCommandService.Instance.Press(_applyButton);
            Console.WriteLine($"[MinimapShift] shifted for {Duration}s");
        }
        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
            Console.WriteLine("[MinimapShift] restored");
        }
    }
}
