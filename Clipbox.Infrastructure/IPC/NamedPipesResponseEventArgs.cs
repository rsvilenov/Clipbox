using System;

namespace Clipbox.Infrastructure.IPC
{
    public class NamedPipesResponseEventArgs : EventArgs
    {
        public string Response { get; set; }
    }
}
