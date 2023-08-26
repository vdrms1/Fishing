using System;
using System.Drawing;

namespace FishingFunBot.Bot.Interfaces
{
    public interface IBiteWatcher
    {
        Action<FishingEvent> FishingEventHandler { set; get; }
        void Reset(Point initialBobberPosition);

        bool IsBite(Point currentBobberPosition);
    }
}