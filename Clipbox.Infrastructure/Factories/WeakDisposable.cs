using System;

namespace Clipbox.Infrastructure.Factories
{
    internal class WeakDisposable<T> : IDisposable
    {
        // The objects decorated with WeakDiposable are not meant to be disposed deterministically,
        // but this class still implements IDisposable to prevent confusion.

        internal WeakDisposable(T instance)
        {
            _value = instance; 
        }

        private T _value;

        internal T Value
        {
            get
            {
                if (_disposedValue)
                {
                    throw new ObjectDisposedException($"Type {typeof(T).FullName}, wrapped in WeakDisposable, has already been disposed.");
                }

                return _value;
            }
        }
            
        private bool _disposedValue = false;

        private void DisposeInternal()
        {
            if (!_disposedValue)
            {   
                // use the backing field as it doesn't throw
                if (_value is IDisposable)
                {
                    // we should never throw from a finalizer
                    try
                    {
                        (Value as IDisposable).Dispose();
                    }
                    catch { }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        ~WeakDisposable()
        {
            DisposeInternal();
        }
    }
}
