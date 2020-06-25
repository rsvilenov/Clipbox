using ClipboxGuard.Services.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboxGuard.Services.Interfaces
{
    public interface IReportSenderService
    {
        void SendReport(string reportText);

        event EventHandler<ReportStateEventArgs> ProgressChanged;
    }
}
