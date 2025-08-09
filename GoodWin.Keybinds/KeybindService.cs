using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoodWin.Keybinds;

public sealed class KeybindService : IKeybindService, IDisposable
{
    private readonly ISteamPathService _steam;
    private FileSystemWatcher? _watcher;
    private Dictionary<string,string> _bindings = new(StringComparer.OrdinalIgnoreCase);
    private List<KeybindEntry> _entries = new();
    private string? _currentPath;

    public KeybindService(ISteamPathService steam)
    {
        _steam = steam;
        Reload();
    }

    public IReadOnlyDictionary<string, string> Bindings => _bindings;

    public IReadOnlyList<KeybindEntry> Entries => _entries;

    public string? CurrentPath => _currentPath;

    public event EventHandler? BindingsChanged;

    public void Reload()
    {
        var path = _steam.SuggestMostRecentDotakeys();
        if (path is null) return;
        _currentPath = path;
        try
        {
            var text = File.ReadAllText(path);
            _entries = DotaKeyvalues.Parse(text).ToList();
            _bindings = _entries.ToDictionary(e => e.Label, e => e.Key ?? "", StringComparer.OrdinalIgnoreCase);
            Watch(path);
            BindingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch { }
    }

    private void Watch(string path)
    {
        _watcher?.Dispose();
        var dir = Path.GetDirectoryName(path);
        var file = Path.GetFileName(path);
        if (dir is null || file is null) return;
        _watcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += (s, e) => OnFileChanged();
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged()
    {
        if (_currentPath is null) return;
        try
        {
            var text = File.ReadAllText(_currentPath);
            _entries = DotaKeyvalues.Parse(text).ToList();
            _bindings = _entries.ToDictionary(e => e.Label, e => e.Key ?? "", StringComparer.OrdinalIgnoreCase);
            BindingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch { }
    }

    public async Task SaveAsync(IEnumerable<KeybindEntry> entries)
    {
        if (_currentPath is null) return;
        try
        {
            var path = _currentPath;
            var text = await File.ReadAllTextAsync(path);
            var backup = path + ".bak";
            File.Copy(path, backup, true);
            var newText = DotaKeyvalues.Serialize(entries, text);
            await File.WriteAllTextAsync(path, newText);
            Reload();
        }
        catch { }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
