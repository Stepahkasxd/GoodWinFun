namespace GoodWin.Keybinds;

public interface ISteamPathService
{
    string? GetSteamRoot();
    IEnumerable<string> EnumerateDotaKeyFiles();
    string? SuggestMostRecentDotakeys();
}
