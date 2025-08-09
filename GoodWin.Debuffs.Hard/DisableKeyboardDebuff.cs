using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.IO;
using System.Media;
using System.Linq;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class DisableKeyboardDebuff : DebuffBase
    {
        private const int Duration = 60;
        public override string Name => "Отключить клавиатуру";
        // блокируем все клавиши, кроме Ctrl/Alt/P для паник-комбо
        private static readonly int[] BlockedVks =
            Enumerable.Range(8, 0x100 - 8)
                      .Except(new[] { 0x50, 0xA2, 0xA3, 0xA4, 0xA5 })
                      .ToArray();

        public override void Apply()
        {
            var path1 = Path.Combine(AppContext.BaseDirectory, "Sounds", "keyboard_off.wav");
            try { using var player = new SoundPlayer(path1); player.Play(); } catch { }
            foreach (var vk in BlockedVks)
                InputHookHost.Instance.BlockKey(vk);
            Console.WriteLine($"[DisableKB] blocked for {Duration}s");
        }
        public override void Remove()
        {
            foreach (var vk in BlockedVks)
                InputHookHost.Instance.UnblockKey(vk);
            var path2 = Path.Combine(AppContext.BaseDirectory, "Sounds", "keyboard_on.wav");
            try { using var player = new SoundPlayer(path2); player.Play(); } catch { }
            Console.WriteLine("[DisableKB] unblocked");
        }
    }
}