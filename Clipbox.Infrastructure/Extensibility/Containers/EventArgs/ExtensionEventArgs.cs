using System;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class ExtensionEventArgs : EventArgs
    {
        public Type ExtensionType { get; set; }
    }
}
