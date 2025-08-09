using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task InitializeCommandsAsync(string path, CancellationToken token)
        {
            var start = DateTime.UtcNow;
            while (!WindowHelper.IsDota2Active())
            {
                token.ThrowIfCancellationRequested();
                if ((DateTime.UtcNow - start).TotalSeconds > 30)
                    throw new TimeoutException("Dota 2 window not found");
                await Task.Delay(500, token);
            }

            await JoyCommandService.Instance.InitializeBindingsAsync(token);
        }
    }
}
