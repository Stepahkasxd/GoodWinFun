using System.IO;
using System.Text.Json;
using GoodWin.Core;

namespace GoodWin.Gui.Services
{
    public class UserSettingsService
    {
        public UserSettings Settings { get; private set; } = new UserSettings();
        private readonly string _file;

        public UserSettingsService(string file)
        {
            _file = file;
            if (File.Exists(_file))
            {
                var json = File.ReadAllText(_file);
                Settings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_file, json);
        }

        public void Reset()
        {
            Settings = new UserSettings();
        }
    }
}
