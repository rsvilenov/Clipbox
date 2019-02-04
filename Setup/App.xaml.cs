using Clipbox.Core.Enums;
using Setup.SetupActions;
using System;
using System.Linq;
using System.Windows;

namespace Setup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string[] startupArgs = Environment.GetCommandLineArgs();
            if (startupArgs.Any(arg => arg == "SHChangeNotify"))
            {
                var dsn = new DeskbandShellNotify();
                dsn.Do();

                Shutdown((int)ProcessExitCodes.Ok);
            }
        }
    }
}
