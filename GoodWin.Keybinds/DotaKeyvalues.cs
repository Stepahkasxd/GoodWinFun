using System.Text.RegularExpressions;

namespace GoodWin.Keybinds;

public static class DotaKeyvalues
{
    private static readonly Regex BlockRegex = new(@"\s*""([^""]+)""\s*\{(.*?)\}", RegexOptions.Singleline);
    private static readonly Regex PairRegex  = new(@"\s*""([^""]+)""\s*""([^""]*)""", RegexOptions.Singleline);

    public static IReadOnlyList<KeybindEntry> Parse(string text)
    {
        var keysMatch = Regex.Match(text, @"""Keys""\s*\{(.*)\}\s*$", RegexOptions.Singleline | RegexOptions.Multiline);
        if (!keysMatch.Success) return Array.Empty<KeybindEntry>();
        var keysBody = keysMatch.Groups[1].Value;
        var result = new List<KeybindEntry>();
        foreach (Match m in BlockRegex.Matches(keysBody))
        {
            var label = m.Groups[1].Value;
            var body  = m.Groups[2].Value;
            var dict  = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match p in PairRegex.Matches(body))
                dict[p.Groups[1].Value] = p.Groups[2].Value;

            result.Add(new KeybindEntry
            {
                Label = label,
                Name = dict.GetValueOrDefault("Name",""),
                Action = dict.GetValueOrDefault("Action",""),
                Key = dict.GetValueOrDefault("Key",""),
                Panel = dict.GetValueOrDefault("Panel",""),
                PanelRow = dict.GetValueOrDefault("PanelRow",""),
                Description = dict.GetValueOrDefault("Description",""),
                Version = dict.GetValueOrDefault("Version",""),
            });
        }
        return result;
    }
}
