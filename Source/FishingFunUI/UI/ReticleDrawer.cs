using System.Drawing;
using System.Drawing.Drawing2D;

namespace FishingFun.UI
{
    public class ReticleDrawer
    {
        public void Draw(Bitmap bmp, Point point)
        {
            var p = bmp.GetPixel(point.X, point.Y);

            bmp.SetPixel(point.X, point.Y, Color.White);

            using (var gr = Graphics.FromImage(bmp))
            {
                gr.SmoothingMode = SmoothingMode.AntiAlias;
                using (var thick_pen = new Pen(Color.White, 2))
                {
                    var cornerSize = 15;
                    var recSize = 40;
                    DrawCorner(thick_pen, gr, new Point(point.X - recSize, point.Y - recSize), cornerSize, cornerSize);
                    DrawCorner(thick_pen, gr, new Point(point.X - recSize, point.Y + recSize), cornerSize, -cornerSize);
                    DrawCorner(thick_pen, gr, new Point(point.X + recSize, point.Y - recSize), -cornerSize, cornerSize);
                    DrawCorner(thick_pen, gr, new Point(point.X + recSize, point.Y + recSize), -cornerSize,
                        -cornerSize);
                }
            }
        }

        private void DrawCorner(Pen pen, Graphics gr, Point corner, int xDiff, int yDiff)
        {
            var lines = new[]
            {
                new Point(corner.X + xDiff, corner.Y),
                corner,
                new Point(corner.X, corner.Y + yDiff)
            };

            gr.DrawLines(pen, lines);
        }
    }
}