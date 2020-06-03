using System;

namespace Clipbox.Infrastructure.IPC
{
    public class NamedPipesResponseEventArgs : EventArgs
    {
        public string ReceivedMessage { get; set; }

        public string Response { get; set; }
    }
}
