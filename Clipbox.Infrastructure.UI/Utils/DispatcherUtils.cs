using System;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI.Utils
{
    public static class DispatcherUtils
    {
        public static void DispatchWhile(Dispatcher dispatcher, Func<bool> condition)
        {

            // wait for the sync task to complete without blocking the dispatcher thread
            while (condition())
            {
                try
                {
                    dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
                }
                // todo: should we give up if the dispatcher processing has been disabled?
                // The only way to know this is by the InvalidOperationException thrown by the dispatcher on Invoke()
                catch (InvalidOperationException)
                {
                    break;
                }
            }
        }
    }
}
