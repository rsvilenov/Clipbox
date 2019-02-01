using System;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    interface INotifyExtentionResolved
    {
        event EventHandler<ExtentionEventArgs> ExtentionResolved;
    }
}
