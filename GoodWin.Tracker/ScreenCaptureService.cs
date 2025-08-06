using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace GoodWin.Tracker
{
    /// <summary>
    /// Захватывает кадры экрана с заданной частотой.
    /// </summary>
    public class ScreenCaptureService : IDisposable
    {
        private readonly Timer _timer;
        private readonly int _intervalMs;

        /// <summary>
        /// Событие, вызываемое при захвате нового кадра.
        /// </summary>
        public event Action<Bitmap>? FrameCaptured;

        public ScreenCaptureService(int fps = 60)
        {
            if (fps <= 0) fps = 60;
            _intervalMs = 1000 / fps;
            _timer = new Timer(Capture, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Начать захват.
        /// </summary>
        public void Start() => _timer.Change(0, _intervalMs);

        /// <summary>
        /// Остановить захват.
        /// </summary>
        public void Stop() => _timer.Change(Timeout.Infinite, Timeout.Infinite);

        private void Capture(object? state)
        {
            try
            {
                var bounds = Screen.PrimaryScreen.Bounds;
                using var bmp = new Bitmap(bounds.Width, bounds.Height);
                using var g = Graphics.FromImage(bmp);
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                FrameCaptured?.Invoke((Bitmap)bmp.Clone());
            }
            catch
            {
                // Игнорируем ошибки захвата
            }
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
        }
    }
}
