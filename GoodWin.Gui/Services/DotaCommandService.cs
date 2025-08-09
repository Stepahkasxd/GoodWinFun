using System;
using System.Threading;
using System.Threading.Tasks;
using GoodWin.Utils;

namespace GoodWin.Gui.Services
{
    public class DotaCommandService
    {
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
        }
    }
}
