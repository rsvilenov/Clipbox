using Clipbox.Services.Enums;
using System;

namespace Clipbox.Services.EventArgsClasses
{
    public class ErrorStateEventArgs : EventArgs
    {
        public ErrorStateEventArgs(ErrorState state, string message = "", string details = "")
        {
            State = state;
            Message = message;
            Details = details;
        }

        public ErrorState State { get; }

        public string Message { get; }

        public string Details { get; }
    }
}
