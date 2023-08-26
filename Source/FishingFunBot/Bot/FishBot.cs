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
        public static readonly ILog logger = LogManager.GetLogger("Fishbot");
        
        private static readonly Random Random = new Random();
        private readonly IBiteWatcher biteWatcher;
        private readonly IBobberFinder bobberFinder;

        private ConsoleKey castKey;
        private readonly ConsoleKey hsKey;
        private bool isEnabled;
        private readonly Stopwatch lureStopwatch = new Stopwatch();
        private readonly int lureTimer = 10;
        private readonly int maxFinshingMinutes = 45;

        private DateTime startTime = DateTime.Now;
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

        private ConsoleKey RodKey { get; set; }

        private ConsoleKey LureKey { get; set; }

        public event EventHandler<FishingEvent> FishingEventHandler;

        [DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        public void Start()
        {
            biteWatcher.FishingEventHandler = e => FishingEventHandler(this, e);

            isEnabled = true;

            // Enable total time stopwatch
            totalTimeStopWatch.Start();

            // Equip the rod
            WowProcess.PressKey(RodKey);

            // Enable lure stopwatch
            lureStopwatch.Start();
            Lure.ApplyLure(RodKey, LureKey);

            while (isEnabled)
                try
                {
                    CheckLureTimer();
                    logger.Info($"Pressing key {castKey} to Cast.");

                    FishingEventHandler?.Invoke(this, new FishingEvent { Action = FishingAction.Cast });
                    WowProcess.PressKey(castKey);

                    Watch(2000);

                    WaitForBite();
                    CheckForStopTimer();
                }
                catch (Exception e)
                {
                    logger.Error(e.ToString());
                    Thread.Sleep(2000);
                }

            logger.Error("Bot has Stopped.");
        }

        private void CheckForStopTimer()
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

        private void CheckLureTimer()
        {
            var ts = lureStopwatch.Elapsed;
            var elapsedMinutes = ts.Minutes;
            var elapsedSeconds = ts.Seconds;

            logger.Info("Checking the lure timer.");

            if ((elapsedMinutes >= lureTimer && elapsedSeconds > 30) || elapsedMinutes > lureTimer)
            {
                Lure.ApplyLure(RodKey, LureKey);
                lureStopwatch.Restart();
            }
            else
            {
                logger.Info($"Lure still active for {lureTimer - elapsedMinutes} min");
            }
        }

        public void SetCastKey(ConsoleKey consoleKey)
        {
            castKey = consoleKey;
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
            if (!((DateTime.Now - startTime).TotalMinutes > 10) || tenMinKey.Count <= 0) return;
            startTime = DateTime.Now;
            logger.Info($"Pressing key {tenMinKey} to run a macro.");
            FishingEventHandler(this, new FishingEvent { Action = FishingAction.Cast });
            foreach (var key in tenMinKey) WowProcess.PressKey(key);
        }

        private void Loot(Point bobberPosition)
        {
            Sleep(900 + Random.Next(0, 225));
            logger.Info("Right clicking mouse to Loot.");
            WowProcess.RightClickMouse(logger, bobberPosition);
            Sleep(1000 + Random.Next(0, 125));
        }

        private static void Sleep(int ms)
        {
            var sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.TotalMilliseconds < ms)
            {
                FlushBuffers();
                Thread.Sleep(100);
            }
        }

        private static void FlushBuffers()
        {
            var log = LogManager.GetLogger("Fishbot");
            if (!(log.Logger is Logger logger)) return;
            foreach (var appender in logger.Appenders)
            {
                var buffered = appender as BufferingAppenderSkeleton;
                buffered?.Flush();
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