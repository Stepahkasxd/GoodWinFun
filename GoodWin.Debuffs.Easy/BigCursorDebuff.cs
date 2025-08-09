using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 0, 999, 60)]
    public class BigCursorDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Большой курсор";

        private readonly int _autoOffButton;
        private readonly int _scaleBigButton;
        private readonly int _scaleResetButton;
        private readonly int _autoOnButton;

        public BigCursorDebuff()
        {
            _autoOffButton = JoyCommandService.Instance.Register("cl_auto_cursor_scale 0");
            _scaleBigButton = JoyCommandService.Instance.Register("cl_cursor_scale 30");
            _scaleResetButton = JoyCommandService.Instance.Register("cl_cursor_scale 1");
            _autoOnButton = JoyCommandService.Instance.Register("cl_auto_cursor_scale 1");
        }

        public override void Apply()
        {
            JoyCommandService.Instance.Press(_autoOffButton);
            JoyCommandService.Instance.Press(_scaleBigButton);
            Console.WriteLine($"[BigCursor] applied for {Duration}s");
        }
        public override void Remove()
        {
            JoyCommandService.Instance.Press(_scaleResetButton);
            JoyCommandService.Instance.Press(_autoOnButton);
            Console.WriteLine("[BigCursor] restored");
        }
    }
}
