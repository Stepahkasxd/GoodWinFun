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
        private readonly Dictionary<Guid, Action<DrawingContext>> _drawActions = new();

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
                            var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
                            NativeMethods.SetWindowLongPtr(
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
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Left = 0;
            Top = 0;
        }

        /// <summary>
        /// Добавить действие рисования, вызываемое каждый кадр.
        /// </summary>
        public Guid AddOverlay(Action<DrawingContext> draw)
        {
            var id = Guid.NewGuid();
            Dispatcher.Invoke(() =>
            {
                _drawActions[id] = draw;
                InvalidateVisual();
            });
            return id;
        }

        /// <summary>
        /// Удалить действие рисования по идентификатору.
        /// </summary>
        public void RemoveOverlay(Guid id)
            => Dispatcher.Invoke(() =>
            {
                _drawActions.Remove(id);
                InvalidateVisual();
            });

        /// <summary>
        /// Очистить все действия рисования.
        /// </summary>
        public void ClearOverlays()
            => Dispatcher.Invoke(() =>
            {
                _drawActions.Clear();
                InvalidateVisual();
            });

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            foreach (var action in _drawActions.Values)
                action(drawingContext);
        }

        private static class NativeMethods
        {
            public const int GWL_EXSTYLE = -20;
            public const int WS_EX_TRANSPARENT = 0x20;
            public const int WS_EX_LAYERED = 0x80000;

            [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
            private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
            private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

            [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
            private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
            private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

            public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
                => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);

            public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
                => IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, dwNewLong);
        }
    }
}
