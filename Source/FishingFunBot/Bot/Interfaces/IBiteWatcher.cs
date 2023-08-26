using System;
using System.Drawing;

namespace FishingFun
{
    public interface IBiteWatcher
    {
        Action<FishingEvent> FishingEventHandler { set; get; }
        void Reset(Point InitialBobberPosition);

        bool IsBite(Point currentBobberPosition);
    }
}