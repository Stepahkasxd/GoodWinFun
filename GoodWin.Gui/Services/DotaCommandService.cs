using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GoodWin.Utils;
using GoodWin.Gui.Views;

namespace GoodWin.Gui.Services
{
    public class DotaCommandService
    {
        public bool ConsoleDebuffsEnabled { get; private set; } = true;

        public async Task InitializeCommandsAsync(CancellationToken token)
        {
            var start = DateTime.UtcNow;
            while (!WindowHelper.IsDota2Active())
            {
                token.ThrowIfCancellationRequested();
                if ((DateTime.UtcNow - start).TotalSeconds > 30)
                    throw new TimeoutException("Dota 2 window not found");
                await Task.Delay(500, token);
            }

            await JoyCommandService.Instance.InitializeBindingsAsync(token);

            if (!JoyCommandService.Instance.IsOperational || !JoyCommandService.Instance.SelfTest())
            {
                ConsoleDebuffsEnabled = false;
                DebugLogService.Log("JoyCommandService unavailable. Console debuffs disabled.");
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var win = new DebuffNotificationWindow("ViGEm недоступен", "Консольные дебаффы отключены");
                    win.Show();
                    Task.Delay(3000).ContinueWith(_ => win.Dispatcher.Invoke(win.Close));
                });
            }
        }
    }
}
