using System.Threading;

namespace Clipbox.Infrastructure.Threading
{
    public static class ThreadValidator
    {
        public static void ValidateSTA()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException("Thread must be STA");
            }
        }
    }
}
