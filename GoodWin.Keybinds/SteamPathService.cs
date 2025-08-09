using Microsoft.Win32;
using System.IO;

namespace GoodWin.Keybinds;

public sealed class SteamPathService : ISteamPathService
{
    public string? GetSteamRoot()
    {
        using var rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        var raw = rk?.GetValue("SteamPath") as string;
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw.Replace('/', '\\');
    }

    public IEnumerable<string> EnumerateDotaKeyFiles()
    {
        var root = GetSteamRoot();
        if (root is null) yield break;
        var glob = Path.Combine(root, "userdata");
        if (!Directory.Exists(glob)) yield break;
        foreach (var id in Directory.EnumerateDirectories(glob))
        {
            var path = Path.Combine(id, @"570\remote\cfg\dotakeys_personal.lst");
            if (File.Exists(path)) yield return path;
        }
    }

    public string? SuggestMostRecentDotakeys()
        => EnumerateDotaKeyFiles()
           .OrderByDescending(p => File.GetLastWriteTimeUtc(p))
           .FirstOrDefault();
}
