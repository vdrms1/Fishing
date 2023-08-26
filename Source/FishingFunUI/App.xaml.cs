using System.IO;
using System.Windows;
using log4net.Config;

namespace FishingFun
{
    public partial class App : Application
    {
        public App()
        {
            XmlConfigurator.Configure(new FileStream("log4net.config", FileMode.Open));
        }
    }
}