using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class ThirdPersonCameraDebuff : DebuffBase
    {
        public override string Name => "Камера от третьего лица";
        private string? _prevDistance;
        public override void Apply()
        {
            _prevDistance = ReadCurrentDistance() ?? "1134";
            InputHookHost.Instance.SendKey((int)Keys.I);
            InputHookHost.Instance.Cmd("dota_camera_distance 2000");
            InputHookHost.Instance.BlockKey((int)Keys.I);
            Console.WriteLine("[ThirdPerson] enabled");
        }
        public override void Remove()
        {
            InputHookHost.Instance.UnblockKey((int)Keys.I);
            var value = _prevDistance ?? "1134";
            InputHookHost.Instance.Cmd($"dota_camera_distance {value}");
            InputHookHost.Instance.SendKey((int)Keys.I);
            Console.WriteLine("[ThirdPerson] disabled");
        }

        private string? ReadCurrentDistance()
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Dota 2", "cfg", "config.cfg");
                if (File.Exists(path))
                {
                    var line = File.ReadLines(path).FirstOrDefault(l => l.Contains("dota_camera_distance"));
                    if (line != null)
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                            return parts[^1];
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
