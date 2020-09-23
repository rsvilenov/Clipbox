using System;

namespace Clipbox.Services.EventArgsClasses
{
    public class DetailedErrorEventArgs : EventArgs
    {
        public DetailedErrorEventArgs(string message, string details = "")
        {
            Message = message;
            Details = details;
        }
        public string Message { get; }

        public string Details { get; }
    }
}
