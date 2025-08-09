using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 0, 999, 60)]
    public class Fps12Debuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "FPS 12";

        private readonly int _applyButton;
        private readonly int _removeButton;

        public Fps12Debuff()
        {
            _applyButton = JoyCommandService.Instance.Register("fps_max 12");
            _removeButton = JoyCommandService.Instance.Register("fps_max 120");
        }
        public override void Apply()
        {
            JoyCommandService.Instance.Press(_applyButton);
            Console.WriteLine($"[FPS12] limited for {Duration}s");
        }
        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
            Console.WriteLine("[FPS12] restored");
        }
    }
}
