using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Clipbox
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Bootstrapper _bootstrapper;

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        private async void Application_Startup(object sender, StartupEventArgs e)
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        {
            _bootstrapper = new Bootstrapper();
            _bootstrapper.PerformStartupSequence();

            // Wire the shutdown sequence to ShutdownStarted
            Application.Current.Dispatcher.ShutdownStarted += (s, args)
                => _bootstrapper.PerformShutdownSequence(false);
        }
    }
}
