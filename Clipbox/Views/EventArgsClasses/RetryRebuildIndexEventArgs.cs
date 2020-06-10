using System;

namespace Clipbox.Views.EventArgsClasses
{
    public class RetryRebuildIndexEventArgs : EventArgs
    {
        public RetryRebuildIndexEventArgs(string failedId)
        {
            FailedId = failedId;
        }

        public string FailedId { get; }
    }
}
