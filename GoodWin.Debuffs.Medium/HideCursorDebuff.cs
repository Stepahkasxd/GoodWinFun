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
        public override void Apply()
        {
            InputHookHost.Instance.Cmd("dota_hide_cursor 1");
            Console.WriteLine($"[HideCursor] hidden for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.Cmd("dota_hide_cursor 0");
            Console.WriteLine("[HideCursor] restored");
        }
    }
}
