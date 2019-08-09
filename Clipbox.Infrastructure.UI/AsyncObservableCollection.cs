using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Data;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI
{
    public class AsyncObservableCollection<T> : ObservableCollection<T>
    {
        private readonly object _lockHandle = new object();
        private readonly List<int> _threadsWithSynchronizationEnabled = new List<int>();

        public AsyncObservableCollection()
        {
        }

        public AsyncObservableCollection(IEnumerable<T> list)
            : base(list)
        {
        }

        public void EnableSyncForCurrentUIThread()
        {
            var dispatcherSyncContext = SynchronizationContext.Current as DispatcherSynchronizationContext;
            if (dispatcherSyncContext == null)
            {
                throw new NotSupportedException("This method should be called on UI threads only.");
            }

            EnableCollectionSynchronization(dispatcherSyncContext);
        }

        private void InvokeSynchronized(Action action)
        {
            lock (_lockHandle)
            {   
                if (SynchronizationContext.Current is DispatcherSynchronizationContext)
                {
                    EnableCollectionSynchronization((DispatcherSynchronizationContext)SynchronizationContext.Current);
                }
                // if we are calling the collection from non-UI threads only, the lock should be enough.

                try
                {
                    action.Invoke();
                }
                catch (NotSupportedException nsex)
                {
                    if (nsex.Source == "PresentationFramework")
                    {
                        throw new InvalidOperationException($"It looks like CollectionSynchronization has not been enabled on the UI thread. Automatic enabling has failed. Consider calling {nameof(EnableCollectionSynchronization)}() on the UI thread.", nsex);
                    }

                    throw;
                }
            }
        }

        private void EnableCollectionSynchronization(DispatcherSynchronizationContext syncContext)
        {
            // Ensure that we are running inside the UI thread, as this is a requirement for BindingOperations.EnableCollectionSynchronization().
            // The documentation explicitly states that "The call must occur on the UI thread."
            // https://docs.microsoft.com/en-us/dotnet/api/system.windows.data.bindingoperations.enablecollectionsynchronization?redirectedfrom=MSDN&view=netframework-4.7.2#System_Windows_Data_BindingOperations_EnableCollectionSynchronization_System_Collections_IEnumerable_System_Object_System_Windows_Data_CollectionSynchronizationCallback_
            if (!_threadsWithSynchronizationEnabled.Contains(Thread.CurrentThread.ManagedThreadId))
            {
                // we should call EnableCollectionSynchronization on each UI thread.
                BindingOperations.EnableCollectionSynchronization(this, _lockHandle);
                _threadsWithSynchronizationEnabled.Add(Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected override void InsertItem(int index, T item)
        {
            InvokeSynchronized(() => base.InsertItem(index, item));
        }

        protected override void RemoveItem(int index)
        {
            InvokeSynchronized(() => base.RemoveItem(index));
        }

        protected override void SetItem(int index, T item)
        {
            InvokeSynchronized(() => base.SetItem(index, item));
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            InvokeSynchronized(() => base.MoveItem(oldIndex, newIndex));
        }

        protected override void ClearItems()
        {
            InvokeSynchronized(() => base.ClearItems());
        }
    }
}
