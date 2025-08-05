using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace GoodWin.Utils
{
    /// <summary>
    /// Singleton transparent overlay window for drawing custom graphics atop any application.
    /// </summary>
    public class OverlayWindow : Window
    {
        private static OverlayWindow? _instance;
        private static readonly object _lock = new();
        private readonly List<Action<DrawingContext>> _drawActions = new();

        /// <summary>
        /// Получить единственный экземпляр окна-оверлея.
        /// </summary>
        public static OverlayWindow Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance != null)
                        return _instance;

                    var ready = new AutoResetEvent(false);

                    var thread = new Thread(() =>
                    {
                        _instance = new OverlayWindow();
                        _instance.SourceInitialized += (s, e) =>
                        {
                            var hwnd = new WindowInteropHelper(_instance).Handle;
                            var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
                            NativeMethods.SetWindowLong(
                                hwnd,
                                NativeMethods.GWL_EXSTYLE,
                                new IntPtr(exStyle.ToInt64() |
                                           NativeMethods.WS_EX_TRANSPARENT |
                                           NativeMethods.WS_EX_LAYERED));
                        };

                        _instance.Show();
                        ready.Set();
                        System.Windows.Threading.Dispatcher.Run();
                    })
                    {
                        IsBackground = true,
                        Name = "OverlayWindowThread",
                        ApartmentState = ApartmentState.STA
                    };

                    thread.Start();
                    ready.WaitOne();

                    return _instance!;
                }
            }
        }

        private OverlayWindow()
        {
            // Настройка окна
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent; // явно WPF-кисть
            Topmost = true;
            ShowInTaskbar = false;
        }

        /// <summary>
        /// Добавить действие рисования, вызываемое каждый кадр.
        /// </summary>
        public void AddOverlay(Action<DrawingContext> draw)
            => Dispatcher.Invoke(() => _drawActions.Add(draw));

        /// <summary>
        /// Очистить все действия рисования.
        /// </summary>
        public void ClearOverlays()
            => Dispatcher.Invoke(() => _drawActions.Clear());

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            foreach (var action in _drawActions)
                action(drawingContext);
        }

        private static class NativeMethods
        {
            public const int GWL_EXSTYLE = -20;
            public const int WS_EX_TRANSPARENT = 0x20;
            public const int WS_EX_LAYERED = 0x80000;

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        }
    }
}
