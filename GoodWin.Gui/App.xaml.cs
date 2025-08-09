using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using GoodWin.Gui.Services;
using GoodWin.Gui.Views;

namespace GoodWin.Gui
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                Services.DebugLogService.Log($"Unhandled UI exception: {e.Exception.Message}");
                e.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    Services.DebugLogService.Log($"Unhandled exception: {ex.Message}");
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!ViGEmBusService.IsDriverPresent())
            {
                if (!File.Exists(ViGEmBusService.InstallerPath))
                {
                    MessageBox.Show("Не найден ViGEmBus.exe", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                var wait = new DriverInstallWindow();
                wait.Show();
                var success = await ViGEmBusService.RunInstallerAsync();
                wait.Close();

                if (!success)
                {
                    MessageBox.Show(
                        "В процессе установки появились ошибки, закройте программу и попробуйте снова",
                        "Установка не прошла успешно",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                var res = MessageBox.Show(
                    "Установка прошла успешно, хотите перезагрузить ПК сейчас? Пока вы не перезагрузите ПК, программа не будет работать корректно",
                    "Установка прошла успешно",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                if (res == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo("shutdown", "/r /t 0")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    Shutdown();
                    return;
                }
            }

            new MainWindow().Show();
        }
    }
}
