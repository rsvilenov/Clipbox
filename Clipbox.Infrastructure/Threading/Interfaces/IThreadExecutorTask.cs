namespace Clipbox.Infrastructure.Threading.Interfaces
{
    public interface IThreadExecutorTask
    {
        void Run();

        void Cancel();
    }
}
