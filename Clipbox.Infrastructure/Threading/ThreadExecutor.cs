using Clipbox.Infrastructure.Threading.Enums;
using System;
using System.Threading;

namespace Clipbox.Infrastructure.Threading
{
    public sealed class ThreadExecutor : IDisposable
    {
        /// <summary>
        /// The time to wait after calling IThreadExecutorTask.Cancel()
        /// and before aborting the thread.
        /// </summary>
        private const int CANCEL_DEFAULT_TIMEOUT_MS = 5000;

        private readonly int _cancelTimeoutMs;

        private readonly Action<CancellationToken> _teTask;

        private readonly int _timeoutMs;

        private readonly Thread _workerThread;

        private Timer _timeoutTimer;

        private Timer _cancellationTimer;

        private readonly ManualResetEvent _workerThreadWaitMre;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private ExecutionStage _currentStage;

        private ExecutionStage _lastStageBeforeCancelRequest;

        private Exception _workerException;

        private volatile bool _isCancelRequested;

        private volatile bool _isStartRequested;

        private volatile bool _isWorkerThreadFinished;

        private volatile bool _isDisposing;

        public ThreadExecutor(Action<CancellationToken> teTask, int timeoutMs, int cancelTimeoutMs = CANCEL_DEFAULT_TIMEOUT_MS)
        {
            _teTask = teTask ?? throw new ArgumentNullException(nameof(teTask));
            _timeoutMs = timeoutMs;
            _cancelTimeoutMs = cancelTimeoutMs;

            _workerThreadWaitMre = new ManualResetEvent(false);
            _cancellationTokenSource = new CancellationTokenSource();

            _workerThread = new Thread(() =>
            {
                _currentStage = ExecutionStage.Started;
                try
                {
                    _teTask.Invoke(_cancellationTokenSource.Token);
                    _currentStage = _currentStage == ExecutionStage.CancelRequested ?
                        ExecutionStage.Cancelled : ExecutionStage.Completed;
                }
                catch (Exception ex)
                {
                    // reduce the chance that the timeout timer will
                    // call IThreadExecutorTask.Cancel() after an exception
                    _isWorkerThreadFinished = true;

                    if (ex is ThreadAbortException && _currentStage == ExecutionStage.AbortFromTimeoutRequested)
                    {
                        _currentStage = ExecutionStage.Aborted;
                    }
                    else
                    {
                        // if the abort was initiated due to an exception in IThreadExecutorTask.Cancel(),
                        // do not set the AbortException to _workerException.
                        if (!(ex is ThreadAbortException && _currentStage == ExecutionStage.CancelRequested))
                        {
                            _workerException = ex;
                        }

                        _currentStage = ExecutionStage.Error;
                    }
                }
                finally
                {
                    _isWorkerThreadFinished = true;
                    _workerThreadWaitMre.Set();
                }
            });
        }

        private void CancellationTimerCallback(object state)
        {
            if (!_isWorkerThreadFinished 
                && !_workerThread.ThreadState.HasFlag(ThreadState.AbortRequested)
                && !_workerThread.ThreadState.HasFlag(ThreadState.Aborted)
                && (_workerThread.ThreadState.HasFlag(ThreadState.Background)
                || _workerThread.ThreadState.HasFlag(ThreadState.Running)
                || _workerThread.ThreadState.HasFlag(ThreadState.StopRequested)
                || _workerThread.ThreadState.HasFlag(ThreadState.WaitSleepJoin)))
            {
                _currentStage = ExecutionStage.AbortFromTimeoutRequested;
                // we will not wait for the code that may run in the thread after
                // the ThreadAbortException has been raised, i.e. "finally" blocks 
                // in the code, running in the thread. The user of that class
                // has the option to wait for the thread's completion, including
                // after an abort has been initiated, by using the Wait() method.
                try { _workerThread.Abort(); } catch { }
            }
        }

        private void TimeoutTimerCallback(object state)
        {
            if (_currentStage == ExecutionStage.Started)
            {
                _workerThreadWaitMre.Set();
                CancelOrAbortOnCancelException();
            }
        }

        private void CancelOrAbortOnCancelException()
        {
            if (_isCancelRequested)
            {
                return;
            }

            _isCancelRequested = true;

            try
            {
                if (!_isWorkerThreadFinished)
                {
                    _cancellationTimer = new Timer(CancellationTimerCallback, null, _cancelTimeoutMs, Timeout.Infinite);
                    
                    // TODO: Race condition: if the timeout elapses at about the same time a worker code exception is thrown,
                    // then we may still call Cancel() after the exception, which is not desirable. Currently, at 3:03 AM,
                    // I cannot think of a proper solution to this problem.
                    if (!_isWorkerThreadFinished)
                    {
                        _lastStageBeforeCancelRequest = _currentStage;
                        _currentStage = ExecutionStage.CancelRequested;
                        _cancellationTokenSource.Cancel(throwOnFirstException: true);
                    }
                }
            }
            catch (Exception ex)
            {
                _workerException = ex;

                try { _workerThread.Abort(); } catch { }

                // do not change the _currentStage to Error here. This will be done in the catch block in the worker thread.
            }
        }

        public Exception GetException()
        {
            if (_currentStage != ExecutionStage.Error)
            {
                throw new InvalidOperationException($"This method should be called only if the execution stage is {nameof(ExecutionStage.Error)}");
            }

            return _workerException;
        }


        public void Start()
        {
            if (_isDisposing)
            {
                throw new ObjectDisposedException(nameof(ThreadExecutor));
            }

            if (!_isStartRequested)
            {
                _isStartRequested = true;
                _timeoutTimer = new Timer(TimeoutTimerCallback, null, _timeoutMs, Timeout.Infinite);
                _workerThread.Start();
            }
        }

        /// <summary>
        /// Waits until the timeout is reached or the task is
        /// finished, cancelled or aborted.
        /// </summary>
        /// <returns>The execution stage at the time this method returns.</returns>
        public ExecutionStage WaitUntilTimeoutReached()
        {
            if (_isDisposing)
            {
                throw new ObjectDisposedException(nameof(ThreadExecutor));
            }

            if (!_isStartRequested)
            {
                throw new InvalidOperationException($"Please call {nameof(Start)} first.");
            }

            _workerThreadWaitMre.WaitOne();

            return _currentStage == ExecutionStage.CancelRequested ?
                _lastStageBeforeCancelRequest : _currentStage;
        }

        public ExecutionStage Wait()
        {
            if (_isDisposing)
            {
                throw new ObjectDisposedException(nameof(ThreadExecutor));
            }

            if (!_isStartRequested)
            {
                throw new InvalidOperationException($"Please call {nameof(Start)} first.");
            }

            if (!_isWorkerThreadFinished)
            {
                _workerThread.Join();
            }

            return _currentStage == ExecutionStage.CancelRequested ?
                _lastStageBeforeCancelRequest : _currentStage;
        }

        public void RequestCancel()
        {
            if (_isDisposing)
            {
                throw new ObjectDisposedException(nameof(ThreadExecutor));
            }

            if (!_isStartRequested)
            {
                throw new InvalidOperationException($"Please call {nameof(Start)} first.");
            }

            CancelOrAbortOnCancelException();
        }

        public void Dispose()
        {
            if (!_isDisposing)
            {
                _isDisposing = true;

                if (!_isWorkerThreadFinished)
                {
                    CancelOrAbortOnCancelException();
                    try { _workerThread.Join(); } catch { }
                }

                _workerThreadWaitMre.Dispose();
                _cancellationTokenSource.Dispose();
            }
        }
    }
}
