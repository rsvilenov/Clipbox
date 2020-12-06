using System;

namespace Clipbox.Views.EventArgsClasses
{
    public class PopupHintResponseEventArgs : EventArgs
    {
        public PopupHintResponseEventArgs(PopupHintResponse response)
        {
            Response = response;
        }

        public PopupHintResponse Response { get; set; }

        public enum PopupHintResponse
        {
            Close,
            Confirm,
            Decline
        }
    }
}
