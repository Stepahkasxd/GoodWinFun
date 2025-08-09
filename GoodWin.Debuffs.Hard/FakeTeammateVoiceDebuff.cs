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
            try
            {
                using var player = new SoundPlayer(path);
                player.Play();
            }
            catch (Exception ex)
            {
                Log($"FakeTeammateVoiceDebuff sound failed ({path}): {ex.Message}");
            }
        }
        public override void Remove() { }

        private static void Log(string message)
        {
            try
            {
                var type = Type.GetType("GoodWin.Gui.Services.DebugLogService, GoodWin.Gui");
                var method = type?.GetMethod("Log", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                method?.Invoke(null, new object[] { message });
            }
            catch
            {
                Console.WriteLine(message);
            }
        }
    }
}
