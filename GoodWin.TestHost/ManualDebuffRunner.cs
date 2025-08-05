using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GoodWin.Core;

namespace GoodWin.TestHost
{
    public static class ManualDebuffRunner
    {
        // P/Invoke для Alt+Tab
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_MENU = 0x12; // Alt
        private const byte VK_TAB = 0x09; // Tab

        private static DebuffsRegistry? _registry;

        public static void Init(DebuffsRegistry registry)
        {
            _registry = registry;
            Console.WriteLine("ManualDebuffRunner initialized. Type \"help\" for commands.");
            Task.Run(CommandLoop);
        }

        private static async Task CommandLoop()
        {
            if (_registry == null) return;

            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var cmd = parts[0].ToLowerInvariant();
                var arg = parts.Length > 1 ? parts[1] : "";

                switch (cmd)
                {
                    case "help":
                        Console.WriteLine("Commands:\n" +
                                          " list            – show all debuffs\n" +
                                          " run <Name>      – run debuff by Name\n" +
                                          " exit            – quit manual mode");
                        break;

                    case "list":
                        foreach (var e in _registry.GetAllEntries())
                            Console.WriteLine($" • {e.Debuff.Name} ({e.Schedule.Phase}, {e.Schedule.DurationSeconds}s)");
                        break;

                    case "run":
                        if (string.IsNullOrEmpty(arg))
                        {
                            Console.WriteLine("Usage: run <DebuffName>");
                            break;
                        }

                        var entry = _registry.GetAllEntries()
                                             .FirstOrDefault(e =>
                                                 string.Equals(e.Debuff.Name, arg, StringComparison.OrdinalIgnoreCase));
                        if (entry == null)
                        {
                            Console.WriteLine($"Debuff \"{arg}\" not found.");
                            break;
                        }

                        Console.WriteLine($"Preparing to run \"{entry.Debuff.Name}\" for {entry.Schedule.DurationSeconds}s...");

                        // 1) Alt+Tab в Dota2
                        keybd_event(VK_MENU, 0, KEYEVENTF_KEYDOWN, 0);
                        keybd_event(VK_TAB, 0, KEYEVENTF_KEYDOWN, 0);
                        keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, 0);
                        keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);

                        // 2) Ждём 2 секунды, чтобы Dota2 успела получить фокус
                        await Task.Delay(2000);

                        // 3) Запускаем Apply и планируем Remove
                        entry.Debuff.Apply();
                        Console.WriteLine($"[Manual] \"{entry.Debuff.Name}\" applied.");

                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(entry.Schedule.DurationSeconds * 1000);
                            entry.Debuff.Remove();
                            Console.WriteLine($"[Manual] \"{entry.Debuff.Name}\" removed.");
                        });

                        break;

                    case "exit":
                        Console.WriteLine("Exiting manual mode.");
                        return;

                    default:
                        Console.WriteLine($"Unknown command: {cmd}. Type help.");
                        break;
                }
            }
        }
    }
}
