namespace Clipbox.Infrastructure.Threading.Enums
{
    public enum ExecutionStage
    {
        Waiting,
        Started,
        Completed,
        CancelRequested,
        Cancelled,
        AbortFromTimeoutRequested,
        Aborted,
        Error
    }
}
