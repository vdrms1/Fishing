using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using FishingFunBot.Bot.Interfaces;
using FishingFunBot.Platform;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;

namespace FishingFunBot.Bot
{
    public class FishingBot
    {
        public static ILog logger = LogManager.GetLogger("Fishbot");

        private static readonly Random random = new Random();
        private readonly IBiteWatcher biteWatcher;
        private readonly IBobberFinder bobberFinder;

        private ConsoleKey castKey;
        private readonly ConsoleKey hsKey;
        private bool isEnabled;
        private readonly Stopwatch lureStopwatch = new Stopwatch();
        private readonly int lureTimer = 10;
        private readonly int maxFinshingMinutes = 45;

        private DateTime StartTime = DateTime.Now;
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly List<ConsoleKey> tenMinKey;
        private readonly Stopwatch totalTimeStopWatch = new Stopwatch();


        public FishingBot(IBobberFinder bobberFinder, IBiteWatcher biteWatcher, ConsoleKey castKey,
            List<ConsoleKey> tenMinKey)
        {
            this.bobberFinder = bobberFinder;
            this.biteWatcher = biteWatcher;
            this.castKey = castKey;
            this.tenMinKey = tenMinKey;

            RodKey = ConsoleKey.D2;
            LureKey = ConsoleKey.D3;
            hsKey = ConsoleKey.D9;

            logger.Info("FishBot Created.");

            FishingEventHandler += (s, e) => { };
        }

        public ConsoleKey RodKey { get; set; }

        public ConsoleKey LureKey { get; set; }

        public event EventHandler<FishingEvent> FishingEventHandler;

        [DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        public void Start()
        {
            biteWatcher.FishingEventHandler = e => FishingEventHandler?.Invoke(this, e);

            isEnabled = true;

            // Enable total time stopwatch
            totalTimeStopWatch.Start();

            // Equip the rod
            WowProcess.PressKey(RodKey);

            // Enable lure stopwatch
            lureStopwatch.Start();
            Lure.applyLure(RodKey, LureKey);

            while (isEnabled)
                try
                {
                    checkLureTimer();
                    logger.Info($"Pressing key {castKey} to Cast.");

                    FishingEventHandler?.Invoke(this, new FishingEvent { Action = FishingAction.Cast });
                    WowProcess.PressKey(castKey);

                    Watch(2000);

                    WaitForBite();
                    checkForStopTimer();
                }
                catch (Exception e)
                {
                    logger.Error(e.ToString());
                    Thread.Sleep(2000);
                }

            logger.Error("Bot has Stopped.");
        }


        public void checkForStopTimer()
        {
            var ts = totalTimeStopWatch.Elapsed;
            var elapsedMinutes = (int)ts.TotalMinutes;

            logger.Info($"Elapsed {elapsedMinutes} out of {maxFinshingMinutes}mins.");

            if (elapsedMinutes > maxFinshingMinutes)
            {
                Stop();
                WowProcess.PressKey(hsKey);
                Thread.Sleep(20000);
                // Sleep computer
                SetSuspendState(false, true, true);
            }
        }

        public void checkLureTimer()
        {
            var ts = lureStopwatch.Elapsed;
            var elapsedMinutes = ts.Minutes;
            var elapsedSeconds = ts.Seconds;

            logger.Info("Checking the lure timer.");

            if ((elapsedMinutes >= lureTimer && elapsedSeconds > 30) || elapsedMinutes > lureTimer)
            {
                Lure.applyLure(RodKey, LureKey);
                lureStopwatch.Restart();
            }
            else
            {
                logger.Info($"Lure still active for {lureTimer - elapsedMinutes} min");
            }
        }

        public void SetCastKey(ConsoleKey castKey)
        {
            this.castKey = castKey;
        }

        private void Watch(int milliseconds)
        {
            bobberFinder.Reset();
            stopwatch.Restart();
            while (stopwatch.ElapsedMilliseconds < milliseconds) bobberFinder.Find();
            stopwatch.Stop();
        }

        public void Stop()
        {
            isEnabled = false;
            totalTimeStopWatch.Reset();
            logger.Error("Bot is Stopping...");
        }

        private void WaitForBite()
        {
            bobberFinder.Reset();

            var bobberPosition = FindBobber();
            if (bobberPosition == Point.Empty) return;

            biteWatcher.Reset(bobberPosition);

            logger.Info("Bobber start position: " + bobberPosition);

            var timedTask = new TimedAction(a => { logger.Info("Fishing timed out!"); }, 25 * 1000, 25);

            // Wait for the bobber to move
            while (isEnabled)
            {
                var currentBobberPosition = FindBobber();
                if (currentBobberPosition == Point.Empty || currentBobberPosition.X == 0) return;

                if (biteWatcher.IsBite(currentBobberPosition))
                {
                    Loot(bobberPosition);
                    PressTenMinKey();
                    return;
                }

                if (!timedTask.ExecuteIfDue()) return;
            }
        }


        private void PressTenMinKey()
        {
            if ((DateTime.Now - StartTime).TotalMinutes > 10 && tenMinKey.Count > 0)
            {
                StartTime = DateTime.Now;
                logger.Info($"Pressing key {tenMinKey} to run a macro.");

                FishingEventHandler?.Invoke(this, new FishingEvent { Action = FishingAction.Cast });

                foreach (var key in tenMinKey) WowProcess.PressKey(key);
            }
        }

        private void Loot(Point bobberPosition)
        {
            Sleep(900 + random.Next(0, 225));
            logger.Info("Right clicking mouse to Loot.");
            WowProcess.RightClickMouse(logger, bobberPosition);
            Sleep(1000 + random.Next(0, 125));
        }

        public static void Sleep(int ms)
        {
            var sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.TotalMilliseconds < ms)
            {
                FlushBuffers();
                //System.Windows.Application.Current?.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new ThreadStart(delegate { }));
                Thread.Sleep(100);
            }
        }

        public static void FlushBuffers()
        {
            var log = LogManager.GetLogger("Fishbot");
            var logger = log.Logger as Logger;
            if (logger != null)
                foreach (var appender in logger.Appenders)
                {
                    var buffered = appender as BufferingAppenderSkeleton;
                    if (buffered != null) buffered.Flush();
                }
        }

        private Point FindBobber()
        {
            var timer = new TimedAction(a => { logger.Info("Waited seconds for target: " + a.ElapsedSecs); }, 1000, 5);

            while (true)
            {
                var target = bobberFinder.Find();
                if (target != Point.Empty || !timer.ExecuteIfDue()) return target;
            }
        }
    }
}