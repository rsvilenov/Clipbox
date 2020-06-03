using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace Clipbox.Infrastructure.IPC
{
    public class NamedPipesServer : IDisposable
    {
        protected const string SUCCESS_RESPONSE = "OK";
        
        public event EventHandler<NamedPipesResponseEventArgs> GotMessage;

        public event EventHandler<NamedPipeErrorEventArgs> ErrorOccurred;

        private readonly NamedPipeServerStream _pipeServer;

        private readonly ManualResetEvent _connectionMre;

        private readonly object _connectionLockHandle = new object();

        private bool _isDisposed;

        public NamedPipesServer(string pipeName)
        {
            _connectionMre = new ManualResetEvent(true);

            var ps = new PipeSecurity();
            var sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            var everyone = sid.Translate(typeof(NTAccount));
            ps.SetAccessRule(new PipeAccessRule(everyone,
                PipeAccessRights.ReadWrite, AccessControlType.Allow));

            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 100, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 512, 512, ps);
            
            _pipeServer.BeginWaitForConnection
                    (new AsyncCallback(WaitForConnectionCallBack), _pipeServer);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                // block other incomming connections
                lock (_connectionMre)
                {
                    // wait for the already existing connection, if any, to disconnect
                    _connectionMre.WaitOne();

                    _connectionMre.Dispose();
                    _pipeServer.Dispose();
                    
                    _isDisposed = true;
                }
            }
        }


        private void WaitForConnectionCallBack(IAsyncResult ar)
        {
            // if we are already in a disposing state,
            // block the thread untill the Dispose() completes
            lock (_connectionMre)
            {
                // and then return
                if (_isDisposed)
                {
                    return;
                }
            }

            // connection processing begins (this is to inform Dispose() that
            // there is an ongoing connection
            _connectionMre.Reset();

            try
            {
                NamedPipeServerStream pipeServer = (NamedPipeServerStream)ar.AsyncState;
                pipeServer.EndWaitForConnection(ar);

                var args = new NamedPipesResponseEventArgs();

                using (StreamReader reader = new StreamReader(pipeServer, Encoding.ASCII, false, 512, leaveOpen: true))
                {
                    args.ReceivedMessage = reader.ReadLine();

                    GotMessage?.Invoke(this, args);
                }

                using (StreamWriter writer = new StreamWriter(pipeServer, Encoding.ASCII, 512, leaveOpen: true))
                {
                    writer.WriteLine(SUCCESS_RESPONSE);
                    writer.WriteLine(args.Response ?? string.Empty);
                    writer.Flush();
                }

                pipeServer.WaitForPipeDrain();

                pipeServer.Disconnect();

                pipeServer.BeginWaitForConnection(
                   new AsyncCallback(WaitForConnectionCallBack), pipeServer);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new NamedPipeErrorEventArgs(ex));
            }
            finally
            {
                // connection processing ends
                _connectionMre.Set();
            }
        }
    }
}
