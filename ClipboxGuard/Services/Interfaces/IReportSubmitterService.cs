using Clipbox.Core.Diagnostics;
using ClipboxGuard.Services.EventArgsClasses;
using System;

namespace ClipboxGuard.Services.Interfaces
{
    public interface IReportSubmitterService
    {
        void SubmitReport(AggregateReport aggregateReport);
        event EventHandler<ReportStateEventArgs> StateChanged;
        string GetError();
    }
}
