using System;
using System.Windows;

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
    }
}
