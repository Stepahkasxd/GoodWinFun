using System.Collections.Generic;

using System.Text.RegularExpressions;

namespace GoodWin.Keybinds;

public static class DotaFriendly
{
    private static readonly Dictionary<string, string> DescriptionToRu = new()
    {
        ["#DOTA_Ability1"] = "Первый скилл",
        ["#DOTA_Ability2"] = "Второй скилл",
        ["#DOTA_Ability3"] = "Третий скилл",
        ["#DOTA_Ability4"] = "Ультимейт",
        ["#DOTA_InventorySlot1"] = "Предмет 1",
        ["#DOTA_InventorySlot2"] = "Предмет 2",
        ["#DOTA_InventorySlot3"] = "Предмет 3",
        ["#DOTA_InventorySlot4"] = "Предмет 4",
        ["#DOTA_InventorySlot5"] = "Предмет 5",
        ["#DOTA_InventorySlot6"] = "Предмет 6",
    };

    public static string MakeFriendly(KeybindEntry e)
    {
        if (DescriptionToRu.TryGetValue(e.Description, out var ru)) return ru;
        if (!string.IsNullOrWhiteSpace(e.Name)) return SplitCamelCase(e.Name);
        if (!string.IsNullOrWhiteSpace(e.Action)) return e.Action.Replace("_", " ");
        return e.Label;
    }

    private static string SplitCamelCase(string s) =>
        Regex.Replace(s, "([a-z])([A-Z])", "$1 $2").Trim();
}

