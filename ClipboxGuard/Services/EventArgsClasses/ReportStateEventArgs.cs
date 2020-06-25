using ClipboxGuard.Enums;
using System;

namespace ClipboxGuard.Services.EventArgsClasses
{
    public class ReportStateEventArgs : EventArgs
    {
        public ReportStateEventArgs(ReportSendState state)
        {
            State = state;
        }

        public ReportSendState State { get; }
    }
}
