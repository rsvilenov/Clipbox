using System;

namespace ClipboxGuard.Services.EventArgsClasses
{
    public class FatalErrorEventArgs : EventArgs
    {
        public FatalErrorEventArgs(string errorText, Exception ex = null)
        {
            ErrorText = errorText;
            Exception = ex;
        }

        public string ErrorText { get; }

        public Exception Exception { get; }
    }
}
