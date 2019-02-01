using System;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class ExtentionEventArgs : EventArgs
    {
        public Type ExtentionType { get; set; }
    }
}
