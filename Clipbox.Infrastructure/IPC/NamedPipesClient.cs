using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Clipbox.Infrastructure.IPC
{
    /// <summary>
    /// WARNING: This implementation is prone to permanent blocking the calling thread,
    /// in case the server doesn't send back anything, due to the blocking nature of Stream.ReadLine()
    /// </summary>
    public class NamedPipesClient
    {
        protected const int CONNECT_TIMEOUT_MS = 5000;

        protected const string SUCCESS_RESPONSE = "OK";

        private readonly string _pipeName;

        public NamedPipesClient(string pipeName)
        {
            _pipeName = pipeName;
        }

        public bool SendMessage(string msg)
        {
            bool success = false;

            using (var client = new NamedPipeClientStream(_pipeName))
            {
                client.Connect(CONNECT_TIMEOUT_MS);

                using (StreamWriter writer = new StreamWriter(client, Encoding.ASCII, 512, leaveOpen: true))
                {
                    writer.WriteLine(msg);
                    writer.Flush();
                }

                client.WaitForPipeDrain();

                using (StreamReader reader = new StreamReader(client, Encoding.ASCII, false, 512, leaveOpen: true))
                {
                    var line = reader.ReadLine();
                    if (line == SUCCESS_RESPONSE)
                    {
                        success = true;
                    }
                }
            }

            return success;
        }
    }
}
