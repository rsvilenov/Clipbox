using ClipboxGuard.Enums;
using System;

namespace ClipboxGuard.Services.EventArgsClasses
{
    public class ReportStateEventArgs : EventArgs
    {
        public ReportStateEventArgs(ReportSubmitState state)
        {
            State = state;
        }

        public ReportSubmitState State { get; }
    }
}
