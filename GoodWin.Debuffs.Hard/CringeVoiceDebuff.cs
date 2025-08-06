using GoodWin.Core;
using System.IO;
using System.Media;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 5)]
    public class CringeVoiceDebuff : DebuffBase, IAudioDebuff
    {
        public override string Name => "Кринж в войс";
        public override void Apply()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Sounds", "cringe.wav");
            try { using var player = new SoundPlayer(path); player.Play(); } catch { }
        }
        public override void Remove() { }
    }
}
