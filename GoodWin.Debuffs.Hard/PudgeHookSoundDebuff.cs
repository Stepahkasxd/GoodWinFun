using System;
using System.IO;
using System.Media;
using GoodWin.Core;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 30, 999, 60)]
    public class PudgeHookSoundDebuff : DebuffBase, IAudioDebuff
    {
        public override string Name => "Меня хукнули?";
        public override void Apply()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Sounds", "pudge_hook.wav");
            if (File.Exists(path))
            {
                using var player = new SoundPlayer(path);
                player.Play();
            }
            else
            {
                Console.WriteLine($"[Audio] File not found: {path}");
            }
        }
        public override void Remove() { }
    }
}