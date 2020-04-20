using System;

namespace Clipbox.Infrastructure.Extensibility.EventArgsClasses
{
    public class ExtensionTypeEventArgs : EventArgs
    {
        public ExtensionTypeEventArgs(Type extensionType)
        {
            ExtensionType = extensionType;
        }

        public Type ExtensionType { get; }
    }
}
