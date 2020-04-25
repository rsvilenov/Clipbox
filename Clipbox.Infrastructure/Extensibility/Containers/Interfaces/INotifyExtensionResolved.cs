using System;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    interface INotifyExtensionResolved
    {
        event EventHandler<ExtensionEventArgs> ExtensionResolved;
    }
}
