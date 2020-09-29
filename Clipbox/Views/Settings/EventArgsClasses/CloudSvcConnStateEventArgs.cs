using Clipbox.Views.Settings.Enums;
using System;

namespace Clipbox.Views.Settings.EventArgsClasses
{
    public class CloudSvcConnStateEventArgs : EventArgs
    {
        public CloudSvcConnStateEventArgs(CloudServiceConnectionState state)
        {
            State = state;
        }

        public CloudServiceConnectionState State { get; }
    }
}
