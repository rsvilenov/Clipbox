using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Clipbox.Infrastructure.Threading
{
    public class SimpleDispatcher : IDisposable
    {
        private BlockingCollection<Action> _delegateCollection;

        private Thread _dispatcherThread;

        bool _disableProcessing;

        public SimpleDispatcher()
        {
            // warning: In order to guarantee that the collection will always use a queue internally (in the current version of .NET it does), 
            // maybe I should construct the collection like this: new BlockingCollection<T>(new ConcurrentQueue<T>()). The only constraint for
            // the internal collection is that it should implement IProducerConsumerCollection<T>.
            _delegateCollection = new BlockingCollection<Action>();

            _dispatcherThread = new Thread(TaskExecuter);
            _dispatcherThread.SetApartmentState(ApartmentState.STA);
            _dispatcherThread.Name = $"STA_{GetType().Name}_{Guid.NewGuid()}";
            _dispatcherThread.Start();
        }

        public Thread Thread
        {
            get { return _dispatcherThread; }
        }

        public void Invoke(Action action)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("The dispatcher has been disposed.");
            }

            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            ManualResetEvent finishedEvent = new ManualResetEvent(false);

            _delegateCollection.Add(new Action(() =>
            {
                try
                {
                    action();
                }
                finally
                {
                    finishedEvent.Set();
                }
            }));

            finishedEvent.WaitOne();
        }

        /// <summary>
        /// TODO: do we need this method?
        /// </summary>
        /// <param name="action"></param>
        public void BeginInvoke(Action action)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("The dispatcher has been disposed.");
            }

            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            _delegateCollection.Add(action);
        }

        private void TaskExecuter()
        {
            foreach (var task in _delegateCollection.GetConsumingEnumerable())
            {
                if (_disableProcessing) continue;
                task();
            }
        }

        #region IDisposable Support
        private bool _disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                try
                {
                    _delegateCollection.CompleteAdding();

                    // Do not allow any already added tasks to be executed, 
                    // as when we join the threads, we may run into the risk
                    // of getting some cross threading issues
                    // (the .NET finalizers run in separate thread)
                    _disableProcessing = true;

                    _dispatcherThread.Join();

                    _delegateCollection.Dispose();
                }
                catch { }

                _disposed = true;
            }
        }


        ~SimpleDispatcher()
        {
            Dispose(false);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
