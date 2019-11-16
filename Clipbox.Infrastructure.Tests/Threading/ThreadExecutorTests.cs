using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.Threading.Enums;
using Clipbox.Infrastructure.Threading.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.Tests.Threading
{
    [TestFixture]
    public class ThreadExecutorTests
    {
        private class ThreadExecutorTask : IThreadExecutorTask
        {
            private readonly Action<ThreadExecutorTask> _runAction;

            private readonly Action<ThreadExecutorTask> _cancelAction;

            public bool CancelRequested { get; set; }

            public ThreadExecutorTask(Action<ThreadExecutorTask> runAction, Action<ThreadExecutorTask> cancelAction)
            {
                _runAction = runAction;
                _cancelAction = cancelAction;
            }

            public void Cancel()
            {
                _cancelAction.Invoke(this);
            }

            public void Run()
            {
                _runAction.Invoke(this);
            }
        }

        [TestCase(1000)]
        public void TestThreadTimeoutCancel(int timeout)
        {
            ThreadExecutorTask tTask = new ThreadExecutorTask((t) =>
            {
                while (!t.CancelRequested)
                {
                    System.Threading.Thread.Sleep(100);
                }
            },
            (t) =>
            {
                t.CancelRequested = true;
            });

            using (ThreadExecutor te = new ThreadExecutor(tTask, timeout))
            {
                te.Start();
                ExecutionStage stage = te.WaitUntilTimeoutReached();

                Assert.AreEqual(ExecutionStage.Started, stage);

                stage = te.Wait();
                Assert.AreEqual(ExecutionStage.Cancelled, stage);

                Assert.Throws<InvalidOperationException>(() => te.GetException());
            }
        }

        [TestCase(1000, true)]
        [TestCase(1000, false)]
        public void TestThreadTimeoutAbort(int timeout, bool throwInCancel)
        {
            ArgumentException exceptionToThrowInCancel = new ArgumentException();

            ThreadExecutorTask tTask = new ThreadExecutorTask((t) =>
            {
                while (!t.CancelRequested)
                {
                    System.Threading.Thread.Sleep(100);
                }
            },
            (t) =>
            {
                if (throwInCancel)
                {
                    throw exceptionToThrowInCancel;
                }

                // do not do anything to cancel the task, so that it be aborted by the ThreadExecutor
            });

            using (ThreadExecutor te = new ThreadExecutor(tTask, timeout))
            {
                te.Start();
                ExecutionStage stage = te.WaitUntilTimeoutReached();

                Assert.AreEqual(ExecutionStage.Started, stage);

                stage = te.Wait();

                if (throwInCancel)
                {
                    Assert.AreEqual(ExecutionStage.Error, stage);
                    Exception ex = te.GetException();

                    Assert.IsInstanceOf(exceptionToThrowInCancel.GetType(), ex);
                }
                else
                {
                    Assert.AreEqual(ExecutionStage.Aborted, stage);
                    Assert.Throws<InvalidOperationException>(() => te.GetException());
                }
            }
        }

        [TestCase(1000)]
        public void TestThreadFinishBeforeTimeout(int timeout)
        {
            ThreadExecutorTask tTask = new ThreadExecutorTask((t) =>
            {
                System.Threading.Thread.Sleep(timeout / 2);
            },
            (t) =>
            {
                // throw some random exception, as
                // this code should not be called.
                throw new ArgumentException();
            });

            using (ThreadExecutor te = new ThreadExecutor(tTask, timeout))
            {
                te.Start();
                ExecutionStage stage = te.WaitUntilTimeoutReached();

                Assert.AreEqual(ExecutionStage.Completed, stage);

                stage = te.Wait();
                Assert.AreEqual(ExecutionStage.Completed, stage);

                Assert.Throws<InvalidOperationException>(() => te.GetException());
            }
        }

        [TestCase(1000, true)]
        [TestCase(1000, false)]
        public void TestThreadErrorBeforeTimeout(int timeout, bool throwInCancel)
        {
            ArgumentException exceptionToThrowInCancel = new ArgumentException();
            ArgumentNullException exceptionToThrowInWorkerCode = new ArgumentNullException();

            ThreadExecutorTask tTask = new ThreadExecutorTask((t) =>
            {
                System.Threading.Thread.Sleep(timeout / 2);
                throw exceptionToThrowInWorkerCode;
            },
            (t) =>
            {
                if (throwInCancel)
                {
                    throw exceptionToThrowInCancel;
                }
            });

            using (ThreadExecutor te = new ThreadExecutor(tTask, timeout))
            {
                te.Start();
                ExecutionStage stage = te.WaitUntilTimeoutReached();

                Assert.AreEqual(ExecutionStage.Error, stage);

                stage = te.Wait();
                Assert.AreEqual(ExecutionStage.Error, stage);

                Exception ex = te.GetException();
                if (throwInCancel)
                {
                    Assert.IsInstanceOf(exceptionToThrowInCancel.GetType(), ex);
                }
                else
                {
                    Assert.IsInstanceOf(exceptionToThrowInWorkerCode.GetType(), ex);
                }
            }
        }

        [TestCase(1000, 3000, true)]
        [TestCase(1000, 3000, false)]
        public void TestThreadErrorAfterTimeoutBeforeAbortRequest(int timeout, int cancelTimeout, bool throwInCancel)
        {
            ArgumentException exceptionToThrowInCancel = new ArgumentException();
            ArgumentNullException exceptionToThrowInWorkerCode = new ArgumentNullException();

            ThreadExecutorTask tTask = new ThreadExecutorTask((t) =>
            {
                System.Threading.Thread.Sleep(timeout * 2);
                throw exceptionToThrowInWorkerCode;
            },
            (t) =>
            {
                if (throwInCancel)
                {
                    throw exceptionToThrowInCancel;
                }
            });

            using (ThreadExecutor te = new ThreadExecutor(tTask, timeout, cancelTimeout))
            {
                te.Start();
                ExecutionStage stage = te.WaitUntilTimeoutReached();

                Assert.AreEqual(ExecutionStage.Started, stage);

                stage = te.Wait();
                Assert.AreEqual(ExecutionStage.Error, stage);

                Exception ex = te.GetException();
                if (throwInCancel)
                {
                    Assert.IsInstanceOf(exceptionToThrowInCancel.GetType(), ex);
                }
                else
                {
                    Assert.IsInstanceOf(exceptionToThrowInWorkerCode.GetType(), ex);
                }
            }
        }

        // some random combinations
        [TestCase(500, 1000, 3000, true, true, true)]
        [TestCase(500, 1000, 3000, false, true, true)]
        [TestCase(500, 1000, 3000, true, false, true)]
        [TestCase(500, 1000, 3000, false, false, false)]
        public void TestDisposeBeforeFinish(int threadRunningTime, int timeout, int cancelTimeout, bool throwInWorkerCode, bool throwInCancel, bool waitTimeoutBeforeDispose)
        {
            ArgumentException exceptionToThrowInCancel = new ArgumentException();
            ArgumentNullException exceptionToThrowInWorkerCode = new ArgumentNullException();

            ThreadExecutorTask tTask = new ThreadExecutorTask((t) =>
            {
                System.Threading.Thread.Sleep(threadRunningTime);
                if (throwInWorkerCode)
                {
                    throw exceptionToThrowInWorkerCode;
                }
            },
            (t) =>
            {
                if (throwInCancel)
                {
                    throw exceptionToThrowInCancel;
                }
            });

            using (ThreadExecutor te = new ThreadExecutor(tTask, timeout, cancelTimeout))
            {
                te.Start();

                if (waitTimeoutBeforeDispose)
                {
                    te.WaitUntilTimeoutReached();
                }

                Assert.DoesNotThrow(() => te.Dispose());
                Assert.Throws<ObjectDisposedException>(() => te.Wait());
            }
        }
    }
}
