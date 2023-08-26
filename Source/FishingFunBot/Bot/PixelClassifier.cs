using System;
using FishingFunBot.Bot.Interfaces;
using log4net;

namespace FishingFunBot.Bot
{
    public class PixelClassifier : IPixelClassifier
    {
        public double ColourMultiplier { get; set; } = 0.5;
        public double ColourClosenessMultiplier { get; set; } = 2.0;

        public bool IsMatch(byte red, byte green, byte blue)
        {
            return IsBigger(red, green) && IsBigger(red, blue) && AreClose(blue, green);
        }

        public void SetConfiguration(bool isWowClasic)
        {
            if (isWowClasic)
            {
                LogManager.GetLogger("Fishbot").Info("Wow Classic configuration");
                ColourMultiplier = 1;
                ColourClosenessMultiplier = 1;
            }
            else
            {
                LogManager.GetLogger("Fishbot").Info("Wow Standard configuration");
            }
        }

        private bool IsBigger(byte red, byte other)
        {
            return red * ColourMultiplier > other;
        }

        private bool AreClose(byte color1, byte color2)
        {
            var max = Math.Max(color1, color2);
            var min = Math.Min(color1, color2);

            return min * ColourClosenessMultiplier > max - 20;
        }
    }
}