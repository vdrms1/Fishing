using System;
using System.Collections.Generic;
using System.Drawing;
using FishingFunBot.Bot.Interfaces;
using FishingFunBot.Platform;
using log4net;

namespace FishingFunBot.Bot
{
    public class PositionBiteWatcher : IBiteWatcher
    {
        private static readonly ILog logger = LogManager.GetLogger("Fishbot");

        // yPositionsAll have all recorded positions which could be used for a different kind of clasifier 
        private List<int> amplitudes = new List<int>();
        private readonly int amplitudeTreashHold = 5;
        private readonly int strikeValue;
        private TimedAction? timer;
        private int yDiff;

        // yPositions have only unique points 
        private List<int> yPositions = new List<int>();

        public PositionBiteWatcher(int strikeValue)
        {
            this.strikeValue = strikeValue;
        }

        public Action<FishingEvent> FishingEventHandler { set; get; } = e => { };

        public void Reset(Point InitialBobberPosition)
        {
            RaiseEvent(new FishingEvent { Action = FishingAction.Reset });

            yPositions = new List<int>();
            amplitudes = new List<int>();
            yPositions.Add(InitialBobberPosition.Y);
            timer = new TimedAction(a =>
            {
                amplitudes.Add(yDiff);
                RaiseEvent(new FishingEvent { Amplitude = yDiff, Action = FishingAction.BobberMove });
            }, 400, 25);
        }

        public bool IsBite(Point currentBobberPosition)
        {
            var positiveTreasholdReached = simplePositiveClasifier();

            if (!yPositions.Contains(currentBobberPosition.Y))
            {
                yPositions.Add(currentBobberPosition.Y);
                yPositions.Sort();
            }

            yDiff = yPositions[(int)((yPositions.Count + 0.5) / 2)] - currentBobberPosition.Y;
            var thresholdReached = yDiff <= -strikeValue;

            if (timer != null) timer.ExecuteIfDue();

            if (thresholdReached || positiveTreasholdReached)
            {
                RaiseEvent(new FishingEvent { Action = FishingAction.Loot });
                if (timer != null) timer.ExecuteNow();
                return true;
            }

            return false;
        }

        public void RaiseEvent(FishingEvent ev)
        {
            FishingEventHandler?.Invoke(ev);
        }


        /* We noticed that the main clasifier doesn't detect sometimes very subtle changes.
         * However there is different pattern which can be observed. Consider this amplitude:
         * 1 0 1 2 1 0 -3 4 5 4 3
         * ^^ As u can see there is change of 7 in amplitude, however they are not negative thus the original
         * Classifier doens't detect them.
         *
         */
        private bool simplePositiveClasifier()
        {
            // First run
            if (amplitudes.Count < 3) return false;

            var currentAmpltitude = amplitudes[amplitudes.Count - 1];
            var lastAmpltitude = amplitudes[amplitudes.Count - 2];
            var last2Ampltitude = amplitudes[amplitudes.Count - 3];

            // This means that we are in positive amplitude 
            if (lastAmpltitude < currentAmpltitude)
            {
                // System.Console.WriteLine($"Checking c:{currentAmpltitude} - l:{lastAmpltitude} = {currentAmpltitude - lastAmpltitude} > {amplitudeTreashHold}");
                if (currentAmpltitude - lastAmpltitude >= amplitudeTreashHold)
                {
                    logger.Info("Detected using positive threshold  clasifier");
                    return true;
                }

                //System.Console.WriteLine($"Checking c:{currentAmpltitude} - l2:{last2Ampltitude} = {currentAmpltitude - last2Ampltitude} > {amplitudeTreashHold}");
                if (currentAmpltitude - last2Ampltitude >= amplitudeTreashHold)
                {
                    logger.Info("Detected using positive threshold  clasifier");
                    return true;
                }
            }

            return false;
        }
    }
}