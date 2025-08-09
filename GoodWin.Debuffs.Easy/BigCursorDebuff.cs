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

        private readonly int _applyButton;
        private readonly int _removeButton;

        public BigCursorDebuff()
        {
            _applyButton = JoyCommandService.Instance.Register("cl_auto_cursor_scale 0; cl_cursor_scale 30");
            _removeButton = JoyCommandService.Instance.Register("cl_cursor_scale 1; cl_auto_cursor_scale 1");
        }

        public override void Apply()
        {
            JoyCommandService.Instance.Press(_applyButton);
            Console.WriteLine($"[BigCursor] applied for {Duration}s");
        }
        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
            Console.WriteLine("[BigCursor] restored");
        }
    }
}
