using ClipboxGuard.Services.Common;
using System;

namespace ClipboxGuard.EventArgsClasses
{
    public class RequestSubmitReportEventArgs : EventArgs
    {
        public RequestSubmitReportEventArgs(ReportAnalysis reportAnalysis)
        {
            ReportAnalysis = reportAnalysis;
        }

        public ReportAnalysis ReportAnalysis { get; private set; }
    }
}
