using System;

namespace Clipbox.Infrastructure.IPC
{
    public class NamedPipeErrorEventArgs : EventArgs
    {
        public NamedPipeErrorEventArgs(Exception ex)
        {
            Exception = ex;
        }

        public Exception Exception { get; }
    }
}
