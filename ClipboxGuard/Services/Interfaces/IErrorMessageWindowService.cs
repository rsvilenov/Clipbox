namespace ClipboxGuard.Services.Interfaces
{
    public interface IErrorMessageWindowService
    {
        void ShowErrorMessageWindow(out bool isRestartRequested);
    }
}
