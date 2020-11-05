using Clipbox.Services.Interfaces;
using Clipbox.Views;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Clipbox.Services
{
    public class AboutWindowService : IAboutWindowService
    {
        private AboutWindow _aboutWindow;
        public void OpenAboutWindow()
        {
            Task.Run(new Action(() =>
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                    new Action(() =>
                    {
                        if (_aboutWindow == null)
                        {
                            _aboutWindow = new AboutWindow();
                            _aboutWindow.Show();
                            _aboutWindow.Closing += _aboutWindow_Closing;
                        }
                        else
                        {
                            _aboutWindow.Activate();
                        }

                    }));
            }));
        }

        private void _aboutWindow_Closing(object sender, CancelEventArgs e)
        {
            _aboutWindow.Closing -= _aboutWindow_Closing;
            _aboutWindow = null;
        }

    }
}
