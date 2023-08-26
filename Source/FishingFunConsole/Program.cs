using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FishingFunBot.Bot;
using FishingFunBot.Platform;
using log4net;
using log4net.Config;

namespace Powershell
{
    public class Program
    {
        private static void Main(string[] args)
        {
            XmlConfigurator.Configure(new FileStream("log4net.config", FileMode.Open));

            var strikeValue = 5;

            var pixelClassifier = new PixelClassifier();
            pixelClassifier.SetConfiguration(WowProcess.IsWowClassic());

            var bobberFinder = new SearchBobberFinder(pixelClassifier);
            var biteWatcher = new PositionBiteWatcher(strikeValue);

            var bot = new FishingBot(bobberFinder, biteWatcher, ConsoleKey.D4, new List<ConsoleKey>());
            bot.FishingEventHandler += (b, e) => LogManager.GetLogger("Fishbot").Info(e);
            
            Thread.Sleep(1500);

            bot.Start();
        }
    }
}