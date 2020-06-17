using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Persistence.PersistenceProviders;
using Clipbox.Win32;
using ClipboxGuard.Services;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace ClipboxGuard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Bootstrapper _bootstrapper;

        protected override void OnStartup(StartupEventArgs e)
        {
            _bootstrapper = new Bootstrapper();
            _bootstrapper.PerformStartupSequence();

            // Wire the shutdown sequence to ShutdownStarted
            Application.Current.Dispatcher.ShutdownStarted += (s, args)
                => _bootstrapper.PerformShutdownSequence(false);

            base.OnStartup(e);
        }
    }
}
