using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 0, 999, 60)]
    public class ViewportScaleDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Ужасное качество";

        private readonly int _applyButton;
        private readonly int _removeButton;

        public ViewportScaleDebuff()
        {
            _applyButton = JoyCommandService.Instance.Register("mat_viewportscale 0.1");
            _removeButton = JoyCommandService.Instance.Register("mat_viewportscale 1");
        }
        public override void Apply()
        {
            JoyCommandService.Instance.Press(_applyButton);
            Console.WriteLine($"[ViewportScale] 0.1 for {Duration}s");
        }
        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
            Console.WriteLine("[ViewportScale] restored");
        }
    }
}
