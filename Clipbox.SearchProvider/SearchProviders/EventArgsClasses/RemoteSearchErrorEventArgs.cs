using System;

namespace Clipbox.Infrastructure.Search.SearchProviders.EventArgsClasses
{
    public class RemoteSearchErrorEventArgs : EventArgs
    {
        public RemoteSearchErrorEventArgs(string message, Exception ex = null)
        {
            Message = message;
            Exception = ex;
        }

        public Exception Exception {get;}
        public string Message { get; }
    }
}
