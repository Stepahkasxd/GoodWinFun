using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.IO;
using System.Media;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class DisableKeyboardDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Отключить клавиатуру";
        public override void Apply()
        {
            var path1 = Path.Combine(AppContext.BaseDirectory, "sound_1.wav");
            try { new SoundPlayer(path1).Play(); } catch { }
            InputHookHost.Instance.BlockAllKeys();
            Console.WriteLine($"[DisableKB] blocked for {Duration}s");
        }
        public override void Remove()
        {
            InputHookHost.Instance.UnblockAllKeys();
            var path2 = Path.Combine(AppContext.BaseDirectory, "sound_2.wav");
            try { new SoundPlayer(path2).Play(); } catch { }
            Console.WriteLine("[DisableKB] unblocked");
        }
    }
}