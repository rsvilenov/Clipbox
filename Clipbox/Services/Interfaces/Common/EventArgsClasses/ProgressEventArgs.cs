using System;

namespace Clipbox.Services.Interfaces.Common.EventArgsClasses
{
    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(int total, int current, bool isSuccess, string currentId)
        {
            Total = total;
            Current = current;
            IsSuccess = isSuccess;
            CurrentId = currentId;
        }
        
        public int Total { get; }

        public int Current { get; }

        public bool IsSuccess { get; }

        public string CurrentId { get; }
    }
}
