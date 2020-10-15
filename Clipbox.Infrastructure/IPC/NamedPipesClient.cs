using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

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

        /// <summary>
        /// Sends a message and waits for the server to process it
        /// </summary>
        /// <param name="msg">The message</param>
        /// <returns>True if the call succeeded</returns>
        public bool SendMessage(string msg)
        {
            return SendMessage(msg, out _);
        }

        /// <summary>
        /// Sends a message and waits for the answer
        /// </summary>
        /// <param name="msg">The message</param>
        /// <param name="answer">The answer</param>
        /// <returns>True if the call succeeded</returns>
        public bool SendMessage(string msg, out string answer)
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

                    answer = reader.ReadToEnd()?.TrimEnd(new char[] { '\r', '\n' });
                }
            }

            return success;
        }

        /// <summary>
        /// Sends a message without waiting for the answer
        /// </summary>
        /// <param name="msg">The message</param>
        /// <param name="answerCallback">When the answer is ready, this delegate will be invoked</param>
        public void SendMessage(string msg, Action<string> answerCallback)
        {
            var client = new NamedPipeClientStream(_pipeName);

            client.Connect(CONNECT_TIMEOUT_MS);

            using (StreamWriter writer = new StreamWriter(client, Encoding.ASCII, 512, leaveOpen: true))
            {
                writer.WriteLine(msg);
                writer.Flush();
            }

            Task.Run(() =>
            {
                try
                {
                    client.WaitForPipeDrain();

                    using (StreamReader reader = new StreamReader(client, Encoding.ASCII, false, 512, leaveOpen: true))
                    {
                        var line = reader.ReadLine();

                        string answer = null;
                        if (line == SUCCESS_RESPONSE)
                        {
                            answer = reader.ReadToEnd()?.TrimEnd(new char[] { '\r', '\n' });
                        }

                        answerCallback?.Invoke(answer);
                    }
                }
                finally
                {
                    client.Dispose();
                }
            });
        }
    }
}
