using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace GoodWin.Gui.Services
{
    public static class DebugLogService
    {
        public static ObservableCollection<string> Entries { get; } = new();

        public static void Log(string message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Entries.Add($"{DateTime.Now:T} - {message}");
                while (Entries.Count > 200) Entries.RemoveAt(0);
            });
        }
    }
}
