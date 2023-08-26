using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Point = System.Drawing.Point;

namespace FishingFun
{
    public partial class ColourConfiguration : Window
    {
        private readonly IPixelClassifier pixelClassifier;

        private Bitmap ScreenCapture = new Bitmap(1, 1);

        public ColourConfiguration(IPixelClassifier pixelClassifier)
        {
            this.pixelClassifier = pixelClassifier;
            RedValue = 100;

            InitializeComponent();

            DataContext = this;
        }

        public int RedValue { get; set; }

        public int ColourMultiplier
        {
            get => (int)(pixelClassifier.ColourMultiplier * 100);
            set => pixelClassifier.ColourMultiplier = (double)value / 100;
        }

        public int ColourClosenessMultiplier
        {
            get => (int)(pixelClassifier.ColourClosenessMultiplier * 100);
            set => pixelClassifier.ColourClosenessMultiplier = (double)value / 100;
        }

        private void RenderColour(bool renderMatchedArea)
        {
            var bitmap = new Bitmap(256, 256);

            var points = new List<Point>();

            for (var b = 0; b < 256; b++)
            for (var g = 0; g < 256; g++)
            {
                if (pixelClassifier.IsMatch((byte)RedValue, (byte)g, (byte)b)) points.Add(new Point(b, g));
                bitmap.SetPixel(b, g, Color.FromArgb(RedValue, g, b));
            }

            if (ScreenCapture == null)
            {
                ScreenCapture = WowScreen.GetBitmap();
                renderMatchedArea = true;
            }

            ColourDisplay.Source = bitmap.ToBitmapImage();
            WowScreenshot.Source = ScreenCapture.ToBitmapImage();

            if (renderMatchedArea)
            {
                Dispatch(() =>
                {
                    MarkEdgeOfRedArea(bitmap, points);
                    ColourDisplay.Source = bitmap.ToBitmapImage();
                });

                Dispatch(() =>
                {
                    var bmp = new Bitmap(ScreenCapture);
                    MarkRedOnBitmap(bmp);
                    WowScreenshot.Source = bmp.ToBitmapImage();
                });
            }
        }

        private void MarkRedOnBitmap(Bitmap bmp)
        {
            for (var x = 0; x < bmp.Width; x++)
            for (var y = 0; y < bmp.Height; y++)
            {
                var pixel = bmp.GetPixel(x, y);
                if (pixelClassifier.IsMatch(pixel.R, pixel.G, pixel.B)) bmp.SetPixel(x, y, Color.Red);
            }
        }

        private static void MarkEdgeOfRedArea(Bitmap bitmap, List<Point> points)
        {
            foreach (var point in points)
            {
                var pointsClose = points.Count(p =>
                    (p.X == point.X && (p.Y == point.Y - 1 || p.Y == point.Y + 1)) ||
                    (p.Y == point.Y && (p.X == point.X - 1 || p.X == point.X + 1)));
                if (pointsClose < 4) bitmap.SetPixel(point.X, point.Y, Color.White);
            }
        }

        private void Red_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LabelRed.Content = RedValue;
            RenderColour(false);
        }

        private void ColourMultiplier_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LabelColourMultiplier.Text =
                $"Red multiplied by {pixelClassifier.ColourMultiplier} must be greater than green and blue.";
        }

        private void ColourClosenessMultiplier_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LabelColourClosenessMultiplier.Text =
                $"How close Green and Blue need to be to each other: {pixelClassifier.ColourClosenessMultiplier}";
        }

        private void Slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            RenderColour(true);
        }

        private void Capture_Click(object sender, RoutedEventArgs e)
        {
            ScreenCapture = WowScreen.GetBitmap();
            RenderColour(true);
        }

        private void Dispatch(Action action)
        {
            Application.Current?.Dispatcher.BeginInvoke((Action)(() => action()));
            Application.Current?.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
    }
}