using GoodWin.Core;
using GoodWin.Utils;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GoodWin.Debuffs.Hard
{
    [DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
    public class MiniGameDebuff : DebuffBase
    {
        private Thread? _uiThread;
        private Window? _window;
        private Canvas? _canvas;
        private Ellipse? _startPort;
        private Path? _currentWire;
        private Point _startPoint;
        private int _completed;
        private const int WireCount = 4;
        private DispatcherTimer? _timer;
        private DateTime _deadline;
        private TextBlock? _timerText;
        private const int TimeLimitSec = 10;

        public override string Name => "Мини-игра";

        public override void Apply()
        {
            InputHookHost.Instance.BlockAllKeys();
            _uiThread = new Thread(ShowWindow)
            {
                IsBackground = true,
                Name = "MiniGameThread"
            };
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
        }

        private void ShowWindow()
        {
            var hwnd = FindWindow(null, "Dota 2");
            int width = 800, height = 600, left = 0, top = 0;
            if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out var r))
            {
                width = r.Right - r.Left;
                height = r.Bottom - r.Top;
                left = r.Left;
                top = r.Top;
            }

            _window = new Window
            {
                Width = width,
                Height = height,
                Left = left,
                Top = top,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false
            };
            _window.KeyDown += Window_KeyDown;
            _window.Closed += (_, __) => InputHookHost.Instance.UnblockAllKeys();

            _canvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)),
                Width = width,
                Height = height
            };
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
            _window.Content = _canvas;

            _timerText = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 24,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetRight(_timerText, 20);
            Canvas.SetTop(_timerText, 20);
            _canvas.Children.Add(_timerText);

            _deadline = DateTime.Now.AddSeconds(TimeLimitSec);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            Timer_Tick(null, EventArgs.Empty);

            BuildPorts(width, height);
            _window.ShowDialog();
        }

        private void BuildPorts(double width, double height)
        {
            if (_canvas == null) return;
            var rnd = new Random();
            var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Yellow, Brushes.Orange, Brushes.Magenta };
            var ids = Enumerable.Range(0, WireCount).ToArray();
            var rightOrder = ids.OrderBy(_ => rnd.Next()).ToArray();

            double spacing = height / (WireCount + 1);
            double leftX = 40;
            double rightX = width - 40;

            for (int i = 0; i < WireCount; i++)
            {
                double y = spacing * (i + 1);
                var left = CreatePort(ids[i], colors[i], leftX, y);
                left.MouseLeftButtonDown += LeftPort_MouseDown;
                _canvas.Children.Add(left);
            }

            for (int i = 0; i < WireCount; i++)
            {
                double y = spacing * (i + 1);
                var right = CreatePort(rightOrder[i], colors[rightOrder[i]], rightX, y);
                right.MouseLeftButtonUp += RightPort_MouseUp;
                _canvas.Children.Add(right);
            }
        }

        private Ellipse CreatePort(int id, Brush color, double x, double y)
        {
            var el = new Ellipse
            {
                Width = 28,
                Height = 28,
                Fill = color,
                Tag = id
            };
            Canvas.SetLeft(el, x - el.Width / 2);
            Canvas.SetTop(el, y - el.Height / 2);
            return el;
        }

        private void LeftPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_canvas == null) return;
            _startPort = sender as Ellipse;
            if (_startPort == null) return;
            _startPoint = e.GetPosition(_canvas);

            _currentWire = new Path
            {
                StrokeThickness = 6,
                Stroke = (_startPort.Fill as Brush) ?? Brushes.White,
                Data = new PathGeometry()
            };
            _canvas.Children.Add(_currentWire);
            _canvas.CaptureMouse();
            e.Handled = true;
        }

        private void Canvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_currentWire == null || _canvas == null) return;
            var pos = e.GetPosition(_canvas);
            _currentWire.Data = new PathGeometry(new[]
            {
                new PathFigure(_startPoint, new PathSegment[]
                {
                    new BezierSegment(
                        new Point(_startPoint.X + 100, _startPoint.Y),
                        new Point(pos.X - 100, pos.Y),
                        pos, true)
                }, false)
            });
        }

        private void Canvas_MouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            if (_canvas == null) return;
            if (_currentWire != null)
                _canvas.Children.Remove(_currentWire);
            _currentWire = null;
            _startPort = null;
            _canvas.ReleaseMouseCapture();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var remaining = _deadline - DateTime.Now;
            if (_timerText != null)
                _timerText.Text = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds)).ToString();
            if (remaining <= TimeSpan.Zero)
            {
                _timer?.Stop();
                _timer = null;
                InputHookHost.Instance.UnblockAllKeys();
                InputHookHost.Instance.Cmd("disconnect");
                _window?.Close();
            }
        }

        private void RightPort_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_canvas == null || _currentWire == null || _startPort == null) return;
            var right = sender as Ellipse;
            if (right != null && Equals(right.Tag, _startPort.Tag))
            {
                var end = GetCenter(right);
                _currentWire.Data = new PathGeometry(new[]
                {
                    new PathFigure(_startPoint, new PathSegment[]
                    {
                        new BezierSegment(
                            new Point(_startPoint.X + 100, _startPoint.Y),
                            new Point(end.X - 100, end.Y),
                            end, true)
                    }, false)
                });
                right.MouseLeftButtonUp -= RightPort_MouseUp;
                _startPort.MouseLeftButtonDown -= LeftPort_MouseDown;
                _completed++;
                if (_completed >= WireCount)
                {
                    _timer?.Stop();
                    _window?.Close();
                }
            }
            else
            {
                _canvas.Children.Remove(_currentWire);
            }

            _currentWire = null;
            _startPort = null;
            _canvas.ReleaseMouseCapture();
            e.Handled = true;
        }

        private static Point GetCenter(Ellipse el)
            => new(Canvas.GetLeft(el) + el.Width / 2, Canvas.GetTop(el) + el.Height / 2);

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.P && Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftAlt) ||
                e.Key == Key.P && Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.RightAlt) ||
                e.Key == Key.P && Keyboard.IsKeyDown(Key.RightCtrl) && Keyboard.IsKeyDown(Key.LeftAlt) ||
                e.Key == Key.P && Keyboard.IsKeyDown(Key.RightCtrl) && Keyboard.IsKeyDown(Key.RightAlt))
            {
                _window?.Close();
            }
        }

        public override void Remove()
        {
            _timer?.Stop();
            _timer = null;
            if (_window != null)
            {
                _window.Dispatcher.Invoke(() => _window.Close());
                _window = null;
            }
            if (_uiThread != null)
            {
                _uiThread.Join();
                _uiThread = null;
            }
            InputHookHost.Instance.UnblockAllKeys();
        }

        private static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        }
    }
}

