using System.IO;

namespace GoodWin.Tracker
{
    /// <summary>
    /// Searches for the Dota 2 installation and creates the
    /// Game State Integration configuration if needed.
    /// </summary>
    public sealed class DotaPathResolver : IDotaPathResolver
    {
        public string? EnsureConfigCreated()
        {
            var cfgDir = FindCfgDirectory();
            if (cfgDir is null)
                return null;

            var gsiDir = Path.Combine(cfgDir, "gamestate_integration");
            if (!Directory.Exists(gsiDir))
                Directory.CreateDirectory(gsiDir);

            var cfgPath = Path.Combine(gsiDir, "gamestate_integration_GoodWinDebuff.cfg");
            if (!File.Exists(cfgPath))
            {
                File.WriteAllText(cfgPath, BuildTemplate());
            }
            return cfgPath;
        }

        private static string? FindCfgDirectory()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    var path = Path.Combine(drive.RootDirectory.FullName,
                        "Steam", "steamapps", "common", "dota 2 beta", "game", "dota", "cfg");
                    if (Directory.Exists(path))
                        return path;
                }
                catch
                {
                    // ignored
                }
            }
            return null;
        }

        private static string BuildTemplate(int port = 3000)
        {
            return
                "\"dota2cfg\"\n" +
                "{\n" +
                $"    \"uri\" \"http://localhost:{port}\"\n" +
                "    \"timeout\" \"5.0\"\n" +
                "    \"buffer\"  \"0.1\"\n" +
                "    \"throttle\" \"0.1\"\n" +
                "    \"heartbeat\" \"30.0\"\n" +
                "    \"data\"\n" +
                "    {\n" +
                "        \"provider\" \"1\"\n" +
                "        \"map\" \"1\"\n" +
                "        \"player\" \"1\"\n" +
                "        \"hero\" \"1\"\n" +
                "        \"abilities\" \"1\"\n" +
                "        \"items\" \"1\"\n" +
                "    }\n" +
                "}";
        }
    }
}
