using ClipboxGuard.Services.Common;

namespace ClipboxGuard.Services.Interfaces
{
    public interface IErrorWindowService
    {
        void ShowErrorWindow(out bool isRestartRequested, ReportAnalysis reportAnalysis = null);
        void ShowErrorWindow(ReportAnalysis reportAnalysis = null);
    }
}
