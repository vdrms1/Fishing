using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace FishingFunBot.Platform
{
    public static class WowScreen
    {
        private static Rectangle _wowWindowdBounds;


        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public static Color GetColorAt(Point pos, Bitmap bmp)
        {
            return bmp.GetPixel(pos.X, pos.Y);
        }

        public static Bitmap GetBitmap()
        {
            var wowProcess = WowProcess.Get();

            if (wowProcess == null) return new Bitmap(0, 0);
            var handle = wowProcess.MainWindowHandle;

            var rect = new Rect();
            GetWindowRect(handle, ref rect);
            var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            _wowWindowdBounds = bounds;
            var result = new Bitmap(bounds.Width / 2, bounds.Height / 2);

            using var graphics = Graphics.FromImage(result);
            graphics.CopyFromScreen(new Point(bounds.Left + bounds.Width / 4, bounds.Top + bounds.Height / 4),
                Point.Empty, bounds.Size);

            return result;
        }

        public static Point GetScreenPositionFromBitmapPostion(Point pos)
        {
            return new Point(pos.X += _wowWindowdBounds.Left + _wowWindowdBounds.Width / 4,
                pos.Y += _wowWindowdBounds.Top + _wowWindowdBounds.Height / 4);
        }

        private struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }
    }
}