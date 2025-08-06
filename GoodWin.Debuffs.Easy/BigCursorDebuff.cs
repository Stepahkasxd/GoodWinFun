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
        public override void Apply()
        {
            InputHookHost.Instance.Cmd("cl_auto_cursor_scale 0");
            InputHookHost.Instance.Cmd("cl_cursor_scale 30");
            Console.WriteLine($"[BigCursor] applied for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.Cmd("cl_cursor_scale 1");
            InputHookHost.Instance.Cmd("cl_auto_cursor_scale 1");
            Console.WriteLine("[BigCursor] restored");
        }
    }
}
