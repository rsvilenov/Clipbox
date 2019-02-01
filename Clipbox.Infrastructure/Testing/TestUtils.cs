using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.Testing
{
    public static class TestUtils
    {
        /// <summary>
        /// The maximum number of threads for the test methods (WaitHandle supports max 64 threads)
        /// </summary>
        public const int MAX_THREADS = 64;


        /// <summary>
        /// Run an action multiple times in parallel.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="numberOfThreads">The max value is 64 as the method uses WaitHandle internally and it has
        /// a limit for the number of threads.</param>
        public static void RunMultipleTimesInParralel(Action action, int numberOfThreads = MAX_THREADS, bool waitTasks = true)
        {
            var events = new ManualResetEvent[numberOfThreads];
            var list = new List<int>();
            for (int i = 0; i < numberOfThreads; i++)
            {
                list.Add(i);
                events[i] = new ManualResetEvent(false);
                int j = i;
                ThreadPool.QueueUserWorkItem(x =>
                {
                    action();
                    events[j].Set();
                }, list[i]);
            }

            if (waitTasks)
            {
                WaitHandle.WaitAll(events);
            }
        }
        
    }
}
