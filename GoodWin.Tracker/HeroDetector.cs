using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Imaging;

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

        private unsafe void ProcessFrame(Bitmap frame)
        {
            try
            {
                using var roi = frame.Clone(_minimapRect, frame.PixelFormat);
                var points = new System.Collections.Generic.List<Point>();
                var data = roi.LockBits(new Rectangle(0, 0, roi.Width, roi.Height), ImageLockMode.ReadOnly, roi.PixelFormat);
                try
                {
                    int stride = data.Stride;
                    int bpp = Image.GetPixelFormatSize(roi.PixelFormat) / 8;
                    byte* ptr = (byte*)data.Scan0;
                    for (int y = 0; y < roi.Height; y++)
                    {
                        byte* row = ptr + y * stride;
                        for (int x = 0; x < roi.Width; x++)
                        {
                            byte* pixel = row + x * bpp;
                            byte b = pixel[0];
                            byte g = pixel[1];
                            byte r = pixel[2];
                            if (g > 200 && r < 100 && b < 100)
                                points.Add(new Point(x, y));
                        }
                    }
                }
                finally
                {
                    roi.UnlockBits(data);
                }

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
