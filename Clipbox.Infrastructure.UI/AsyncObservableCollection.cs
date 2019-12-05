using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows.Data;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI
{
    public class AsyncObservableCollection<T> : ObservableCollection<T>
    {
        // For the values of this constants, please refer to
        // https://github.com/microsoft/referencesource/blob/master/System/compmod/system/collections/objectmodel/observablecollection.cs
        // As it is stated there, IndexerName must agree with Binding.IndexerName.
        // It is declared there separately as "Items[]" in order to avoid referencing PresentationFramework.dll,
        // however in our class we do not suffer from the same limitation, so we can just get the
        // corresponding const value from Binding.IndexerName
        private const string COUNT_STRING = nameof(Count);
        private const string INDEXER_NAME = Binding.IndexerName;

        private readonly object _baseOperationslockHandle = new object();

        // We will suspend the collection change notification during the bulk operations,
        // and we have to make sure that the calling threads do not suspend/release each other's notifications.
        private readonly object _bulkOperationsLockHandle = new object();

        private readonly List<int> _threadsWithSynchronizationEnabled = new List<int>();

        private bool _areChangeNotificationEventsSuspended = false;

        public AsyncObservableCollection()
        {
        }

        public AsyncObservableCollection(IEnumerable<T> list)
            : base(list)
        {
        }

        public void EnableSyncForCurrentUIThread()
        {
            if (!(SynchronizationContext.Current is DispatcherSynchronizationContext))
            {
                throw new NotSupportedException("This method should be called on UI threads only.");
            }

            EnableCollectionSynchronization();
        }

        private void InvokeSynchronized(Action action)
        {
            lock (_baseOperationslockHandle)
            {   
                if (SynchronizationContext.Current is DispatcherSynchronizationContext)
                {
                    EnableCollectionSynchronization();
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
                        throw new InvalidOperationException($"It looks like CollectionSynchronization has not been enabled on the UI thread. Automatic enabling has not happened. Consider calling {nameof(EnableCollectionSynchronization)}() on the UI thread.", nsex);
                    }

                    throw;
                }
            }
        }

        private void EnableCollectionSynchronization()
        {
            // Ensure that we are running inside the UI thread, as this is a requirement for BindingOperations.EnableCollectionSynchronization().
            // The documentation explicitly states that "The call must occur on the UI thread."
            // https://docs.microsoft.com/en-us/dotnet/api/system.windows.data.bindingoperations.enablecollectionsynchronization?redirectedfrom=MSDN&view=netframework-4.7.2#System_Windows_Data_BindingOperations_EnableCollectionSynchronization_System_Collections_IEnumerable_System_Object_System_Windows_Data_CollectionSynchronizationCallback_
            if (!_threadsWithSynchronizationEnabled.Contains(Thread.CurrentThread.ManagedThreadId))
            {
                // we should call EnableCollectionSynchronization on each UI thread.
                BindingOperations.EnableCollectionSynchronization(this, _baseOperationslockHandle);
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

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_areChangeNotificationEventsSuspended)
            {
                base.OnCollectionChanged(e);
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (_areChangeNotificationEventsSuspended 
                && (e.PropertyName == "Count" || e.PropertyName == "Items[]"))
            {
                return;
            }

            base.OnPropertyChanged(e);
        }

        public void AddRange(IList<T> itemList)
        {
            if (itemList == null)
            {
                throw new ArgumentNullException(nameof(itemList));
            }

            lock(_bulkOperationsLockHandle)
            {
                _areChangeNotificationEventsSuspended = true;

                foreach (T item in itemList)
                {
                    Add(item);
                }

                _areChangeNotificationEventsSuspended = false;

                OnPropertyChanged(new PropertyChangedEventArgs(COUNT_STRING));
                OnPropertyChanged(new PropertyChangedEventArgs(INDEXER_NAME));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itemList));
            }
        }

        public void RemoveRange(IList<T> itemList)
        {
            if (itemList == null)
            {
                throw new ArgumentNullException(nameof(itemList));
            }

            lock (_bulkOperationsLockHandle)
            {

                _areChangeNotificationEventsSuspended = true;

                foreach (T item in itemList)
                {
                    Remove(item);
                }

                _areChangeNotificationEventsSuspended = false;

                OnPropertyChanged(new PropertyChangedEventArgs(COUNT_STRING));
                OnPropertyChanged(new PropertyChangedEventArgs(INDEXER_NAME));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, itemList));
            }
        }
    }
}
