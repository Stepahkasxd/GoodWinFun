using GoodWin.Core;
using GoodWin.Utils;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class MiniGameDebuff : DebuffBase
    {
        private Thread? _uiThread;
        private Window? _window;
        public override string Name => "Мини-игра";
        public override void Apply()
        {
            InputHookHost.Instance.BlockAllKeys();
            _uiThread = new Thread(() =>
            {
                _window = new Window
                {
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true,
                    ResizeMode = ResizeMode.NoResize,
                    Title = "Мини-игра"
                };
                int clicks = 0;
                var btn = new Button { Content = "Кликни 5 раз" };
                btn.Click += (s, e) =>
                {
                    clicks++;
                    btn.Content = $"Осталось {5 - clicks}";
                    if (clicks >= 5)
                        _window.Close();
                };
                _window.Content = btn;
                _window.Closed += (s, e) => InputHookHost.Instance.UnblockAllKeys();
                _window.ShowDialog();
            });
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.IsBackground = true;
            _uiThread.Start();
        }
        public override void Remove()
        {
            if (_window != null)
            {
                _window.Dispatcher.Invoke(() => _window.Close());
                _window = null;
            }
            InputHookHost.Instance.UnblockAllKeys();
        }
    }
}
