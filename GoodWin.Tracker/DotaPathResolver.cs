using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace GoodWin.Tracker
{
    /// <summary>
    /// Searches for the Dota 2 installation and creates the
    /// Game State Integration configuration if needed.
    /// </summary>
    public sealed class DotaPathResolver : IDotaPathResolver
    {
        private string? _manualRoot;

        public string? EnsureConfigCreated(string? manualRoot, int port)
        {
            if (!string.IsNullOrWhiteSpace(manualRoot))
                _manualRoot = manualRoot;

            if (_manualRoot != null && !IsValidRoot(_manualRoot))
                return null;

            var cfgDir = _manualRoot != null ? ResolveManualCfgDirectory(_manualRoot) : FindCfgDirectory();
            if (cfgDir is null)
                return null;

            var gsiDir = Path.Combine(cfgDir, "gamestate_integration");
            if (!Directory.Exists(gsiDir))
                Directory.CreateDirectory(gsiDir);

            var cfgPath = Path.Combine(gsiDir, "gamestate_integration_GoodWinDebuff.cfg");
            File.WriteAllText(cfgPath, BuildTemplate(port));
            return cfgPath;
        }

        private static string? ResolveManualCfgDirectory(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
                return null;

            var exePath = Path.Combine(root, "game", "bin", "win64", "dota2.exe");
            if (!File.Exists(exePath))
                return null;

            if (root.EndsWith(Path.Combine("game", "dota", "cfg"), StringComparison.OrdinalIgnoreCase))
                return Directory.Exists(root) ? root : null;

            var candidate = Path.Combine(root, "game", "dota", "cfg");
            return Directory.Exists(candidate) ? candidate : null;
        }

        public bool IsValidRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
                return false;
            var exePath = Path.Combine(root, "game", "bin", "win64", "dota2.exe");
            return File.Exists(exePath);
        }

        private static string? FindCfgDirectory()
        {
            foreach (var root in EnumerateLibraries())
            {
                try
                {
                    var path = Path.Combine(root, "steamapps", "common", "dota 2 beta", "game", "dota", "cfg");
                    if (Directory.Exists(path))
                        return path;
                }
                catch
                {
                    // ignored
                }
            }

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

        private static IEnumerable<string> EnumerateLibraries()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var root = key?.GetValue("SteamPath") as string;
            if (string.IsNullOrWhiteSpace(root))
                yield break;

            root = root.Replace('/', '\\');
            yield return root;

            var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf))
                yield break;

            foreach (var lib in ParseLibraryFolders(vdf))
                yield return lib;
        }

        private static IEnumerable<string> ParseLibraryFolders(string vdfPath)
        {
            foreach (var line in File.ReadLines(vdfPath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;

                var match = Regex.Match(trimmed, @"^""(?:path|contentid|\d+)""\s*""(?<p>[^""]+)""");
                if (match.Success)
                {
                    var path = match.Groups["p"].Value
                        .Replace("\\\\", "\\")
                        .Replace('/', '\\');
                    yield return path;
                }
            }
        }

        private static string BuildTemplate(int port)
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
