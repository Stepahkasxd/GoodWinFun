using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace GoodWin.Gui.Services
{
    public static class ViGEmBusService
    {
        private static readonly string[] RequiredFiles =
        {
            "nefconw.exe",
            "ViGEmBus.inf",
            "ViGEmBus.pdb",
            "ViGEmBus.sys"
        };

        private static readonly string DriverFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Nefarius Software Solutions", "ViGEm Bus Driver");

        public static string InstallerPath =>
            Path.Combine(AppContext.BaseDirectory, "Installer", "ViGEmBus.exe");

        public static bool IsDriverPresent()
            => CheckService() || CheckRegistry() || CheckFiles();

        private static bool CheckService()
        {
            try
            {
                return ServiceController.GetServices()
                    .Any(s => s.ServiceName.Equals("ViGEmBus", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckRegistry()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Services\\ViGEmBus");
                return key is not null;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckFiles()
        {
            try
            {
                return RequiredFiles.All(f => File.Exists(Path.Combine(DriverFolder, f)));
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> RunInstallerAsync()
        {
            if (!File.Exists(InstallerPath)) return false;
            try
            {
                var psi = new ProcessStartInfo(InstallerPath, "/quiet /norestart")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc is null) return false;
                await proc.WaitForExitAsync();
                return IsDriverPresent();
            }
            catch
            {
                return false;
            }
        }
    }
}
