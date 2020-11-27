using Clipbox.Core.Diagnostics;
using ClipboxGuard.Enums;

namespace ClipboxGuard.Services.Common
{
    public class ReportAnalysis
    {
        public ReportAnalysis(ErrorOrigin errorOrigin, string errorDetails, AggregateReport aggregateReport)
        {
            ErrorOrigin = errorOrigin;
            ErrorDetails = errorDetails;
            AggregateReport = aggregateReport;
        }

        public ErrorOrigin ErrorOrigin { get; }
        
        public string ErrorDetails { get; }

        public AggregateReport AggregateReport { get; set; }
    }
}
