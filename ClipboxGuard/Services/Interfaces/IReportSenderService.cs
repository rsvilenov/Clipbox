using ClipboxGuard.Services.EventArgsClasses;
using System;

namespace ClipboxGuard.Services.Interfaces
{
    public interface IReportSenderService
    {
        void SendReport(string reportText);

        event EventHandler<ReportStateEventArgs> ProgressChanged;
    }
}
