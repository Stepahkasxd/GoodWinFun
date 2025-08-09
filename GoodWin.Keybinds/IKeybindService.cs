using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoodWin.Keybinds;

public interface IKeybindService
{
    IReadOnlyDictionary<string, string> Bindings { get; }
    IReadOnlyList<KeybindEntry> Entries { get; }
    event EventHandler? BindingsChanged;
    void Reload();
    Task SaveAsync(IEnumerable<KeybindEntry> entries);
    string? CurrentPath { get; }
}
