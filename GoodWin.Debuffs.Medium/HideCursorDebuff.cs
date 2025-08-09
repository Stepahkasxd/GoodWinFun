using GoodWin.Core;
using GoodWin.Utils;
using System;

namespace GoodWin.Debuffs.Medium
{
    [DebuffSchedule(DebuffPhase.Medium, 0, 999, 60)]
    public class HideCursorDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Скрыть курсор";

        private readonly int _applyButton;
        private readonly int _removeButton;

        public HideCursorDebuff()
        {
            _applyButton = JoyCommandService.Instance.Register("dota_hide_cursor 1");
            _removeButton = JoyCommandService.Instance.Register("dota_hide_cursor 0");
        }
        public override void Apply()
        {
            JoyCommandService.Instance.Press(_applyButton);
            Console.WriteLine($"[HideCursor] hidden for {Duration}s");
        }
        public override void Remove()
        {
            JoyCommandService.Instance.Press(_removeButton);
            Console.WriteLine("[HideCursor] restored");
        }
    }
}
