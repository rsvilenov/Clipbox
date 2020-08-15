using System;

namespace Clipbox.Views.Settings.EventArgsClasses
{
    public class ConfirmationEventArgs : EventArgs
    {
        public ConfirmationEventArgs(bool isConfirmed)
        {
            IsConfirmed = isConfirmed;
        }

        public bool IsConfirmed { get; }
    }
}
