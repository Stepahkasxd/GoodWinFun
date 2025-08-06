using GoodWin.Core;
using System.IO;
using System.Media;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 5)]
    public class FakeTeammateVoiceDebuff : DebuffBase, IAudioDebuff
    {
        public override string Name => "Голос тиммейта";
        public override void Apply()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Sounds", "fake_teammate.wav");
            try { using var player = new SoundPlayer(path); player.Play(); } catch { }
        }
        public override void Remove() { }
    }
}
