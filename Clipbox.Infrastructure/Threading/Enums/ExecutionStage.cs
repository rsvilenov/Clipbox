namespace Clipbox.Infrastructure.Threading.Enums
{
    public enum ExecutionStage
    {
        Waiting,
        Started,
        TimeoutReached,
        Completed,
        CancelRequested,
        Cancelled,
        AbortFromTimeoutRequested,
        Aborted,
        Error
    }
}
