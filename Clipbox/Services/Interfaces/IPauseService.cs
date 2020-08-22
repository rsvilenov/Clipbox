namespace Clipbox.Services.Interfaces
{
    public interface IPauseService
    {
        void PauseClipboardTrackingFor(long milliseconds);

        void ResumeClipboardTracking();

        bool IsClipboardTrackingPaused { get; }

        double RemainingPauseMilliseconds { get; }
    }
}
