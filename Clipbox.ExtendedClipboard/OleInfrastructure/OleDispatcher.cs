using Clipbox.Infrastructure.Threading;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Clipbox.ExtendedClipboard.OleInftastructure
{
    internal class OleDispatcher : IDisposable
    {

        SimpleDispatcher _dispatcher;

        internal OleDispatcher(SimpleDispatcher dispatcher)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            _dispatcher = dispatcher;
        }

        private OleDataTransferService _oleService;
        
        internal void InvokeOLE(Func<OleDataTransferService, int> oleOperation, bool noRetry = false)
        {
            // lazy initialize the ole subsystem
            if (_oleService == null)
            {
                _oleService = new OleDataTransferService();
                _dispatcher.Invoke(() => _oleService.Initialize()); // mirrored with Uninitialize() in the Dispose(bool) override
            }

            _dispatcher.Invoke(() =>
            {
                /// <summary>
                /// There is a bug with some clipboard locking issues...
                /// https://referencesource.microsoft.com/#PresentationCore/Core/CSharp/System/Windows/Clipboard.cs
                /// </summary>
                const int OleRetryCount = 10;

                const int OleRetryDelay = 100;

                int hr = 0;

                for (int currentAttempt = 1; currentAttempt <= OleRetryCount; currentAttempt++)
                {
                    hr = oleOperation.Invoke(_oleService);

                    // if the operation succeeds or if we do not want to retry, stop here
                    if (hr >= 0 || noRetry)
                    {
                        break;
                    }
                    Thread.Sleep(OleRetryDelay);
                }

                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            });
        }
        
        public void Dispose()
        {
            _dispatcher.Invoke(() =>
            {
                if (_oleService != null) { _oleService.Uninitialize(); }
            });
        }
    }
}
