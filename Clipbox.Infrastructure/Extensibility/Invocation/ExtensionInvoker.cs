using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.Infrastructure.Extensibility.EventArgsClasses;
using System;
using System.Runtime.ExceptionServices;
using System.Security;

namespace Clipbox.Infrastructure.Extensibility.Invocation
{
    public class ExtensionInvoker<T> : IDisposable 
        where T : IClipboxExtention
    {
        private bool _isDisposed;

        private T _extensionInstance;

        public event EventHandler<ExtensionTypeEventArgs> ExtensionInvoked;

        public Type ExtensionType { get; }

        public ExtensionInvoker(T extensionInstance)
        {
            if (extensionInstance == null)
            {
                throw new ArgumentNullException();
            }

            _extensionInstance = extensionInstance;
            ExtensionType = extensionInstance.GetType();
        }

        /// <summary>
        /// Invokes the extention. The exact type of the instance in the action delegate
        /// can be specified as a generic parameter.
        /// </summary>
        /// <typeparam name="TExtention"></typeparam>
        /// <param name="action"></param>
        public void Invoke<TExtention>(Action<TExtention> action)
            where TExtention : IClipboxExtention
        {
            Invoke(action);
        }

        /// <summary>
        /// Invokes the extention. The type of the instance in the action delegate
        /// is the general type of the extentions in the container.
        /// </summary>
        /// <param name="action"></param>
        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void Invoke(Action<T> action)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("The extention has already been disposed of.");
            }

            if (_extensionInstance.Equals(default(T)))
            {
                throw new InvalidOperationException("It looks like the extention has already failed.");
            }

            ExtensionInvoked?.Invoke(this, new ExtensionTypeEventArgs(ExtensionType));
            
            try
            {
                action(_extensionInstance);
            }
            catch (Exception ex)
            {
                Exception exceptionOnDispose = null;
                if (_extensionInstance is IDisposable)
                {
                    try { (_extensionInstance as IDisposable).Dispose(); }
                    catch (Exception dex) { exceptionOnDispose = dex; }
                }

                // be intollerat to failure. Each failure results in the extention to be invalidated.
                _extensionInstance = default(T);

                string exceptionMessage = $"{ExtensionType.FullName} [{ExtensionType.Assembly.FullName}] failed.";
                if (exceptionOnDispose != null)
                {
                    throw new ClipboxExtentionException(exceptionMessage, ExtensionType, new AggregateException(ex, exceptionOnDispose));
                }
                else
                {
                    throw new ClipboxExtentionException(exceptionMessage, ExtensionType, ex);
                }
            }
        }

        public void Dispose()
        {
            if (_extensionInstance is IDisposable)
            {
                try { (_extensionInstance as IDisposable).Dispose(); }
                catch (Exception dex)
                {
                    string exceptionMessage = $"{ExtensionType.FullName} [{ExtensionType.Assembly.FullName}] failed to dispose.";
                    // I know the guidelines but I would rather let this exception crash the process
                    // than swallow it.
                    throw new ClipboxExtentionException(exceptionMessage, ExtensionType, dex);
                }

                _extensionInstance = default(T);
                _isDisposed = true;
            }
        }
    }
}
