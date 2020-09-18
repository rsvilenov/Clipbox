using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.Network
{
    public static class NetworkUtils
    {
        public static bool CheckIsEndpointReachable(string uriString, int port, int timeoutMs = 5000)
        {
            Task<bool> task = Task<bool>.Factory.StartNew(() =>
            {
                try
                {
                    Uri uri = new Uri(uriString);
                    IPAddress[] ipList = Dns.GetHostAddresses(uri.Host);

                    foreach (IPAddress ip in ipList)
                    {
                        Socket s = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);

                        s.SendTimeout = timeoutMs;
                        s.ReceiveTimeout = timeoutMs;
                        s.Connect(ip, port);
                        return true;
                    }
                }
                catch(SocketException)
                {
                    return false;
                }

                return false;
            });

            bool success = task.Wait(timeoutMs);
            if (success)
            {
                return task.Result;
            }
            else
            {
                return false;
            }
        }
    }
}
