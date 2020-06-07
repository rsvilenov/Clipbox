using Clipbox.Services.Interfaces.Common.EventArgsClasses;
using System;

namespace Clipbox.Services.Interfaces.Common
{
    public interface IProvideProgressInfo
    {
        event EventHandler<ProgressEventArgs> ProgressChanged;
    }
}
