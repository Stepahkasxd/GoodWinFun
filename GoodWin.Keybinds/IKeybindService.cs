namespace GoodWin.Keybinds;

public interface IKeybindService
{
    IReadOnlyDictionary<string, string> Bindings { get; }
    event EventHandler? BindingsChanged;
    void Reload();
}
