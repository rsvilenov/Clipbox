using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.Infrastructure.Persistence;
using System;
using System.Runtime.ExceptionServices;
using System.Security;

namespace Clipbox.Infrastructure.Extensibility.Invocation
{
    public class ExtentionInvoker<T> : IDisposable 
        where T : IClipboxExtention
    {
        private bool _isDisposed;

        private T _extentionInstance;

        private Type _extentionType;

        public Type ExtentionType
        {
            get
            {
                return _extentionType;
            }
        }

        public ExtentionInvoker(T extentionInstance)
        {
            if (extentionInstance == null)
            {
                throw new ArgumentNullException();
            }

            _extentionInstance = extentionInstance;
            _extentionType = extentionInstance.GetType();
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

            if (_extentionInstance.Equals(default(T)))
            {
                throw new InvalidOperationException("It looks like the extention has already failed.");
            }

            // this may slow us down
            AppDataUtils.WriteToApplicationData("lastInvokedExtention.log", $"{_extentionType.FullName} [{_extentionType.Assembly.FullName}]");

            try
            {
                action(_extentionInstance);
            }
            catch (Exception ex)
            {
                AppDataUtils.WriteToApplicationData("lastFailedExtention.log", $"{_extentionType.FullName} [{_extentionType.Assembly.FullName}]");
                Exception exceptionOnDispose = null;
                if (_extentionInstance is IDisposable)
                {
                    try { (_extentionInstance as IDisposable).Dispose(); }
                    catch (Exception dex) { exceptionOnDispose = dex; }
                }

                // be intollerat to failure. Each failure results in the extention to be invalidated.
                _extentionInstance = default(T);

                string exceptionMessage = $"{_extentionType.FullName} [{_extentionType.Assembly.FullName}] failed.";
                if (exceptionOnDispose != null)
                {
                    throw new ClipboxExtentionException(exceptionMessage, _extentionType, new AggregateException(ex, exceptionOnDispose));
                }
                else
                {
                    throw new ClipboxExtentionException(exceptionMessage, _extentionType, ex);
                }
            }
        }

        public void Dispose()
        {
            if (_extentionInstance is IDisposable)
            {
                try { (_extentionInstance as IDisposable).Dispose(); }
                catch (Exception dex)
                {
                    string exceptionMessage = $"{_extentionType.FullName} [{_extentionType.Assembly.FullName}] failed to dispose.";
                    // I know the guidelines but I would rather let this exception crash the process
                    // than swallow it.
                    throw new ClipboxExtentionException(exceptionMessage, _extentionType, dex);
                }

                _extentionInstance = default(T);
                _isDisposed = true;
            }
        }
    }
}
