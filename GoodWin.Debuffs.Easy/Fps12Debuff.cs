using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.IO;
using System.Linq;

namespace GoodWin.Debuffs.Easy
{
    [DebuffSchedule(DebuffPhase.Easy, 0, 999, 60)]
    public class Fps12Debuff : DebuffBase
    {
        private const int Duration = 60;
        private string? _prevFps;
        public override string Name => "FPS 12";
        public override void Apply()
        {
            _prevFps = ReadCurrentFps() ?? "120";
            InputHookHost.Instance.Cmd("fps_max 12");
            Console.WriteLine($"[FPS12] limited for {Duration}s");
        }
        public override void Remove()
        {
            var value = _prevFps ?? "120";
            InputHookHost.Instance.Cmd($"fps_max {value}");
            Console.WriteLine("[FPS12] restored");
        }

        private string? ReadCurrentFps()
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Dota 2", "cfg", "video.txt");
                if (File.Exists(path))
                {
                    var line = File.ReadLines(path).FirstOrDefault(l => l.Contains("fps_max"));
                    if (line != null)
                    {
                        var parts = line.Split('"');
                        if (parts.Length >= 2)
                            return parts[1];
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
