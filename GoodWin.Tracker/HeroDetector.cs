using System;
using System.Drawing;
using System.Linq;

namespace GoodWin.Tracker
{
    /// <summary>
    /// Определяет положение героя на миникарте и переводит его в экранные координаты.
    /// </summary>
    public class HeroDetector : IDisposable
    {
        private readonly ScreenCaptureService _capture;
        private readonly Rectangle _minimapRect;

        /// <summary>
        /// Событие, уведомляющее о новой позиции героя в координатах экрана.
        /// </summary>
        public event Action<Point>? HeroPositionUpdated;

        public HeroDetector(ScreenCaptureService capture, Rectangle minimapRect)
        {
            _capture = capture;
            _minimapRect = minimapRect;
            _capture.FrameCaptured += ProcessFrame;
        }

        private void ProcessFrame(Bitmap frame)
        {
            try
            {
                var roi = frame.Clone(_minimapRect, frame.PixelFormat);
                var points = new System.Collections.Generic.List<Point>();
                for (int y = 0; y < roi.Height; y++)
                {
                    for (int x = 0; x < roi.Width; x++)
                    {
                        var color = roi.GetPixel(x, y);
                        if (color.G > 200 && color.R < 100 && color.B < 100)
                        {
                            points.Add(new Point(x, y));
                        }
                    }
                }
                roi.Dispose();
                if (points.Count == 0)
                    return;

                var avgX = (int)points.Average(p => p.X);
                var avgY = (int)points.Average(p => p.Y);
                var screenPoint = new Point(_minimapRect.Left + avgX, _minimapRect.Top + avgY);
                HeroPositionUpdated?.Invoke(screenPoint);
            }
            catch
            {
                // ignored
            }
            finally
            {
                frame.Dispose();
            }
        }

        public void Start() => _capture.Start();
        public void Stop() => _capture.Stop();

        public void Dispose()
        {
            _capture.FrameCaptured -= ProcessFrame;
            _capture.Dispose();
        }
    }
}
