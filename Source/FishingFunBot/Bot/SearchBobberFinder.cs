using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using FishingFunBot.Bot.Interfaces;
using FishingFunBot.Platform;
using log4net;

namespace FishingFunBot.Bot
{
    public class SearchBobberFinder : IBobberFinder, IImageProvider
    {
        private static readonly ILog logger = LogManager.GetLogger("Fishbot");
        private readonly IPixelClassifier pixelClassifier;

        private Bitmap bitmap = new Bitmap(1, 1);

        private Point previousLocation;

        public SearchBobberFinder(IPixelClassifier pixelClassifier)
        {
            this.pixelClassifier = pixelClassifier;
            BitmapEvent += (s, e) => { };
        }

        public void Reset()
        {
            previousLocation = Point.Empty;
        }

        public Point Find()
        {
            bitmap = WowScreen.GetBitmap();

            var best = Score.ScorePoints(FindRedPoints());

            if (previousLocation != Point.Empty && best == null)
            {
                previousLocation = Point.Empty;
                best = Score.ScorePoints(FindRedPoints());
            }

            previousLocation = Point.Empty;
            if (best != null) previousLocation = best.point;

            BitmapEvent?.Invoke(this,
                new BobberBitmapEvent { Point = new Point(previousLocation.X, previousLocation.Y), Bitmap = bitmap });

            bitmap.Dispose();

            return previousLocation == Point.Empty
                ? Point.Empty
                : WowScreen.GetScreenPositionFromBitmapPostion(previousLocation);
        }

        public event EventHandler<BobberBitmapEvent> BitmapEvent;

        private List<Score> FindRedPoints()
        {
            var points = new List<Score>();

            var hasPreviousLocation = previousLocation != Point.Empty;

            // search around last found location
            var minX = Math.Max(hasPreviousLocation ? previousLocation.X - 40 : 0, 0);
            var maxX = Math.Min(hasPreviousLocation ? previousLocation.X + 40 : bitmap.Width, bitmap.Width);
            var minY = Math.Max(hasPreviousLocation ? previousLocation.Y - 40 : 0, 0);
            var maxY = Math.Min(hasPreviousLocation ? previousLocation.Y + 40 : bitmap.Height, bitmap.Height);

            //System.Diagnostics.Debug.WriteLine($"Search from X {minX}-{maxX}, Y {minY}-{maxY}");

            var sw = new Stopwatch();
            sw.Start();

            for (var x = minX; x < maxX; x++)
            for (var y = minY; y < maxY; y++)
                ProcessPixel(points, x, y);
            sw.Stop();

            if (sw.ElapsedMilliseconds > 200)
            {
                var prevText = hasPreviousLocation ? " using previous location" : "";
                Debug.WriteLine($"Red points found: {points.Count} in {sw.ElapsedMilliseconds}{prevText}.");
            }

            if (points.Count > 1000)
            {
                logger.Error("Error: Too much red in this image, adjust the configuration !");
                points.Clear();
            }

            return points;
        }

        private void ProcessPixel(List<Score> points, int x, int y)
        {
            var p = bitmap.GetPixel(x, y);

            var isMatch = pixelClassifier.IsMatch(p.R, p.G, p.B);

            if (isMatch)
            {
                points.Add(new Score { point = new Point(x, y) });
                bitmap.SetPixel(x, y, Color.Red);
            }
        }

        private class Score
        {
            public int count;
            public Point point;

            public static Score? ScorePoints(List<Score> points)
            {
                foreach (var p in points)
                    p.count = points.Where(s => Math.Abs(s.point.X - p.point.X) < 10) // + or - 10 pixels horizontally
                        .Where(s => Math.Abs(s.point.Y - p.point.Y) < 10) // + or - 10 pixels vertically
                        .Count();

                var best = points.OrderByDescending(s => s.count).FirstOrDefault();

                if (best != null)
                {
                    //System.Diagnostics.Debug.WriteLine($"best score: {best.count} at {best.point.X},{best.point.Y}");
                }
                else
                {
                    Debug.WriteLine("No red found");
                }

                return best;
            }
        }
    }
}