using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Clipbox.Infrastructure.Testing;

namespace Clipbox.Infrastructure.UI.Tests
{
    [TestFixture]
    public class SlidingTimerTests
    {
        private SlidingTimer _timer;

        [SetUp]
        public void SetUp()
        {

        }
        

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void SlidingTimer_Elapsed_ShouldFireOnce(int timerIntervalMs)
        {
            int elapsedFireCount = 0;
            
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            DispatcherFrame frame = new DispatcherFrame(exitWhenRequested: true);

            dispatcher.BeginInvoke(new Action(() =>
            {
                _timer = new SlidingTimer
                {
                    IntervalMs = timerIntervalMs
                };

                _timer.Elapsed += (s, e) =>
                {
                    elapsedFireCount++;
                    Task.Delay(timerIntervalMs * 10).ContinueWith((t) => frame.Continue = false);
                };

                // push hard
                TestUtils.RunMultipleTimesInParralel(() =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        _timer.Slide();
                        Thread.Sleep(i * 10);
                    }
                }, TestUtils.MAX_THREADS, false);
            }));

            // run the actual WPF message loop
            Dispatcher.PushFrame(frame);

            Assert.AreEqual(1, elapsedFireCount);
        }
    }
}
