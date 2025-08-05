using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;

namespace GoodWin.Utils
{
    public static class WindowHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Возвращает true, если в фокусе окно процесса dota2.exe.
        /// </summary>
        public static bool IsDota2Active()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                var proc = Process.GetProcessById((int)pid);
                return proc.ProcessName.Equals("dota2", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmd);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        private const int SW_RESTORE = 9;

        public static bool IsDota2Running()
        {
            try
            {
                var procs = Process.GetProcessesByName("dota2");
                return procs.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool ActivateDotaWindow()
        {
            try
            {
                var proc = Process.GetProcessesByName("dota2").FirstOrDefault();
                if (proc == null || proc.MainWindowHandle == IntPtr.Zero) return false;
                if (IsIconic(proc.MainWindowHandle)) ShowWindow(proc.MainWindowHandle, SW_RESTORE);
                return SetForegroundWindow(proc.MainWindowHandle);
            }
            catch
            {
                return false;
            }
        }
    }
}
