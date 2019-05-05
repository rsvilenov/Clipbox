using Clipbox.Infrastructure.Threading;
using System;
using System.Threading;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI.Utils
{
    public class OwnThreadDispatcher : IDisposable
    {
        private Dispatcher _dispatcher;

        public OwnThreadDispatcher()
        {
            using (AutoResetEvent are = new AutoResetEvent(false))
            {
                StaUtils.StartStaTask(() =>
                {
                    _dispatcher = Dispatcher.CurrentDispatcher;
                    are.Set();
                    Dispatcher.Run();
                });

                are.WaitOne();
            }
            
            IsRunning = true;
        }

        public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method)
        {
            return _dispatcher.BeginInvoke(priority, method);
        }

        public object Invoke(DispatcherPriority priority, Delegate method)
        {
            return _dispatcher.Invoke(priority, method);
        }

        public bool IsRunning { get; private set; }
        
        public void Dispose()
        {
            _dispatcher.InvokeShutdown();
            IsRunning = false;
        }
    }
}
