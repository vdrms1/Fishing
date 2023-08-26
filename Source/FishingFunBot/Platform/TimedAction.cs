using System;
using System.Diagnostics;

namespace FishingFunBot.Platform
{
    public class TimedAction
    {
        public Action<TimedAction> action;
        public int actionTimeoutMs;
        public Stopwatch maxTime = new Stopwatch();
        public int maxTimeSecs;
        public Stopwatch stopwatch = new Stopwatch();

        public TimedAction(Action<TimedAction> action, int actionTimeoutMs, int maxTimeSecs)
        {
            this.action = action;
            this.actionTimeoutMs = actionTimeoutMs;
            this.maxTimeSecs = maxTimeSecs;
            stopwatch.Start();
            maxTime.Start();
        }

        public int ElapsedSecs => (int)maxTime.Elapsed.TotalSeconds;

        public void ExecuteNow()
        {
            action(this);
        }

        public bool ExecuteIfDue()
        {
            if (stopwatch.Elapsed.TotalMilliseconds > actionTimeoutMs)
            {
                action(this);
                stopwatch.Reset();
                stopwatch.Start();
            }

            return maxTime.Elapsed.TotalSeconds < maxTimeSecs;
        }
    }
}