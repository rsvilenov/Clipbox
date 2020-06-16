using System;

namespace ClipboxGuard.Services.EventArgsClasses
{
    public class RestartFatalErrorEventArgs : FatalErrorEventArgs
    {
        public RestartFatalErrorEventArgs(string errorText, Exception ex = null)
            : base(errorText, ex)
        {
        }

        public bool RestartProcess { get; set; } = true;
    }
}
