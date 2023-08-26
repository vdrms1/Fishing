using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Point = System.Drawing.Point;
using Timer = System.Timers.Timer;

namespace FishingFun
{
    public partial class MainWindow : Window, IAppender
    {
        private readonly IBiteWatcher biteWatcher;

        private readonly IBobberFinder bobberFinder;

        private FishingBot? bot;
        private Thread? botThread;
        private Point lastPoint = Point.Empty;
        private readonly IPixelClassifier pixelClassifier;
        private readonly ReticleDrawer reticleDrawer = new ReticleDrawer();
        private bool setImageBackgroundColour = true;
        private readonly int strikeValue = 5; // this is the depth the bobber must go for the bite to be detected
        private readonly Timer WindowSizeChangedTimer;

        public MainWindow()
        {
            InitializeComponent();

            ((Logger)FishingBot.logger.Logger).AddAppender(this);

            DataContext = LogEntries = new ObservableCollection<LogEntry>();
            pixelClassifier = new PixelClassifier();
            pixelClassifier.SetConfiguration(WowProcess.IsWowClassic());

            bobberFinder = new SearchBobberFinder(pixelClassifier);

            var imageProvider = bobberFinder as IImageProvider;
            if (imageProvider != null) imageProvider.BitmapEvent += ImageProvider_BitmapEvent;

            biteWatcher = new PositionBiteWatcher(strikeValue);

            WindowSizeChangedTimer = new Timer { AutoReset = false, Interval = 100 };
            WindowSizeChangedTimer.Elapsed += SizeChangedTimer_Elapsed;
            CardGrid.SizeChanged += MainWindow_SizeChanged;
            Closing += (s, e) => botThread?.Abort();

            KeyChooser.CastKeyChanged += (s, e) =>
            {
                Settings.Focus();
                bot?.SetCastKey(KeyChooser.CastKey);
            };
        }

        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            Dispatch(() =>
                LogEntries.Insert(0, new LogEntry
                {
                    DateTime = DateTime.Now,
                    Message = loggingEvent.RenderedMessage
                })
            );
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Reset the timer so it only fires 100ms after the user stop dragging the window.
            WindowSizeChangedTimer.Stop();
            WindowSizeChangedTimer.Start();
        }

        private void SizeChangedTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatch(() =>
            {
                flyingFishAnimation.AnimationWidth = (int)ActualWidth;
                flyingFishAnimation.AnimationHeight = (int)ActualHeight;
                LogGrid.Height = LogFlipper.ActualHeight;
                GraphGrid.Height = GraphFlipper.ActualHeight;
                GraphGrid.Visibility = Visibility.Visible;
                GraphFlipper.IsFlipped = true;
                LogFlipper.IsFlipped = true;
                GraphFlipper.IsFlipped = false;
                LogFlipper.IsFlipped = false;
            });
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            bot?.Stop();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            new ColourConfiguration(pixelClassifier).Show();
        }

        private void CastKey_Click(object sender, RoutedEventArgs e)
        {
            KeyChooser.Focus();
        }

        private void FishingEventHandler(object sender, FishingEvent e)
        {
            Dispatch(() =>
            {
                switch (e.Action)
                {
                    case FishingAction.BobberMove:
                        if (!GraphFlipper.IsFlipped) Chart.Add(e.Amplitude);
                        break;

                    case FishingAction.Loot:
                        flyingFishAnimation.Start();
                        LootingGrid.Visibility = Visibility.Visible;
                        break;

                    case FishingAction.Cast:
                        Chart.ClearChart();
                        LootingGrid.Visibility = Visibility.Collapsed;
                        flyingFishAnimation.Stop();
                        setImageBackgroundColour = true;
                        break;
                }

                ;
            });
        }

        private void SetImageVisibility(Image imageForVisible, Image imageForCollapsed, bool state)
        {
            imageForVisible.Visibility = state ? Visibility.Visible : Visibility.Collapsed;
            imageForCollapsed.Visibility = !state ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetButtonStates(bool isBotRunning)
        {
            Dispatch(() =>
            {
                Play.IsEnabled = isBotRunning;
                Stop.IsEnabled = !Play.IsEnabled;
                SetImageVisibility(PlayImage, PlayImage_Disabled, Play.IsEnabled);
                SetImageVisibility(StopImage, StopImage_Disabled, Stop.IsEnabled);
            });
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (bot == null)
            {
                WowProcess.PressKey(ConsoleKey.Spacebar);
                Thread.Sleep(1500);

                SetButtonStates(false);
                botThread = new Thread(BotThread);
                botThread.Start();

                // Hide cards after 10 minutes
                var timer = new Timer { Interval = 1000 * 60 * 10, AutoReset = false };
                timer.Elapsed += (s, ev) => Dispatch(() => LogFlipper.IsFlipped = GraphFlipper.IsFlipped = true);
                timer.Start();
            }
        }

        public void BotThread()
        {
            bot = new FishingBot(bobberFinder, biteWatcher, KeyChooser.CastKey,
                new List<ConsoleKey> { ConsoleKey.D5, ConsoleKey.D6 });
            bot.FishingEventHandler += FishingEventHandler;
            bot.Start();

            bot = null;
            SetButtonStates(true);
        }

        private void ImageProvider_BitmapEvent(object sender, BobberBitmapEvent e)
        {
            Dispatch(() =>
            {
                SetBackgroundImageColour(e);
                reticleDrawer.Draw(e.Bitmap, e.Point);
                var bitmapImage = e.Bitmap.ToBitmapImage();
                e.Bitmap.Dispose();
                Screenshot.Source = bitmapImage;
            });
        }

        private void SetBackgroundImageColour(BobberBitmapEvent e)
        {
            if (setImageBackgroundColour)
            {
                setImageBackgroundColour = false;
                ImageBackground.Background = e.Bitmap.GetBackgroundColourBrush();
            }
        }

        private void Dispatch(Action action)
        {
            Application.Current?.Dispatcher.BeginInvoke((Action)(() => action()));
            Application.Current?.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
    }
}