using System.IO;
using System.Linq;
using System.Threading;
using GoodWin.Utils;

namespace GoodWin.Gui.Services
{
    public class DotaConfigService
    {
        private static readonly (string File, string Content)[] Configs = new[]
        {
            ("fps_max_enable.cfg", "fps_max 30"),
            ("fps_max_disable.cfg", "fps_max 120"),
            ("HideHUD_enable.cfg", "hud_toggle_visibility"),
            ("HideHUD_disable.cfg", "hud_toggle_visibility"),
            ("MinimapShift_enable.cfg", "dota_minimap_position_option 1"),
            ("MinimapShift_disable.cfg", "dota_minimap_position_option 0"),
            ("HideHealthbars_enable.cfg", "dota_hud_healthbars 0"),
            ("HideHealthbars_disable.cfg", "dota_hud_healthbars 1"),
        };

        private static readonly (string Key, string File)[] Bindings = new[]
        {
            ("4", "fps_max_enable.cfg"),
            ("5", "fps_max_disable.cfg"),
            ("[", "HideHUD_enable.cfg"),
            ("]", "HideHUD_disable.cfg"),
            ("6", "MinimapShift_enable.cfg"),
            ("7", "MinimapShift_disable.cfg"),
            ("ScrollLock", "HideHealthbars_enable.cfg"),
            ("Pause", "HideHealthbars_disable.cfg"),
        };

        public void InitializeConfigs(string path)
        {
            Directory.CreateDirectory(path);
            foreach (var (file, content) in Configs)
            {
                var full = Path.Combine(path, file);
                if (!File.Exists(full))
                {
                    File.WriteAllText(full, content);
                }
            }
        }

        public bool ConfigsExist(string path)
        {
            return Configs.All(c => File.Exists(Path.Combine(path, c.File)));
        }

        public void InitializeCommands(string path)
        {
            if (!ConfigsExist(path)) return;
            while (!WindowHelper.IsDota2Active())
            {
                Thread.Sleep(500);
            }

            const int ConsoleKey = 0xDC;
            const int EnterKey = 0x0D;
            InputHookHost.Instance.SendKey(ConsoleKey);
            Thread.Sleep(100);
            foreach (var (key, file) in Bindings)
            {
                InputHookHost.Instance.SendText($"bind \"{key}\" \"exec {file}\"");
                Thread.Sleep(50);
                InputHookHost.Instance.SendKey(EnterKey);
                Thread.Sleep(50);
            }
            InputHookHost.Instance.SendKey(ConsoleKey);
        }
    }
}
