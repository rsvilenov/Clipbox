using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.Threading
{
    public static class StaUtils
    {
        public static Task<T> StartStaTask<T>(Func<T> func)
        {
            // https://stackoverflow.com/questions/16720496/set-apartmentstate-on-a-task
            var tcs = new TaskCompletionSource<T>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static Task StartStaTask(Action action)
        {
            return StartStaTask<bool>(() => 
            {
                action();
                return true;
            });
        }
    }
}
