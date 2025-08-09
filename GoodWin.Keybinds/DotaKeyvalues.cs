using System.Collections.Generic;

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

    public static string Serialize(IEnumerable<KeybindEntry> entries, string originalText)
    {
        var dict = entries.ToDictionary(e => e.Label, StringComparer.OrdinalIgnoreCase);
        var keysMatch = Regex.Match(originalText, @"""Keys""\s*\{(.*)\}\s*$", RegexOptions.Singleline | RegexOptions.Multiline);
        if (!keysMatch.Success) return originalText;
        var body = keysMatch.Groups[1].Value;
        var replaced = BlockRegex.Replace(body, m =>
        {
            var label = m.Groups[1].Value;
            var block = m.Groups[2].Value;
            if (!dict.TryGetValue(label, out var entry)) return m.Value;
            var newBlock = Regex.Replace(block, "(\"Key\"\\s*\")([^\"]*)(\")", $"$1{entry.Key}$3");
            return m.Value.Replace(block, newBlock);
        });
        return originalText[..keysMatch.Groups[1].Index] + replaced + originalText[(keysMatch.Groups[1].Index + keysMatch.Groups[1].Length)..];
    }
}
