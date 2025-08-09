using GoodWin.Core;
using GoodWin.Utils;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 4, 10, 60)]
    public class MirrorMapDebuff : DebuffBase
    {
        public override string Name => "Отзеркалить карту";

        private readonly int _applyButton;
        private readonly int _removeButton;

        public MirrorMapDebuff()
        {
            _applyButton = JoyCommandService.Instance.Register("dota_minimap_position_option 1");
            _removeButton = JoyCommandService.Instance.Register("dota_minimap_position_option 0");
        }

        public override void Apply()
        {
            JoyCommandService.Instance.Press(_applyButton);
        }

        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
        }
    }
}
