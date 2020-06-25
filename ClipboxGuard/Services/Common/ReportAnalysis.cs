using ClipboxGuard.Enums;

namespace ClipboxGuard.Services.Common
{
    public class ReportAnalysis
    {
        public ReportAnalysis(ErrorOrigin errorOrigin, string errorDetails)
        {
            ErrorOrigin = errorOrigin;
            ErrorDetails = errorDetails;
        }

        public ErrorOrigin ErrorOrigin { get; }
        
        public string ErrorDetails { get; }
    }
}
