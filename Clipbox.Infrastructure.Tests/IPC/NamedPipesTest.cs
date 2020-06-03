using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Testing;
using NUnit.Framework;
using System;
using System.Threading;

namespace Clipbox.Infrastructure.Tests.IPC
{
    [TestFixture]
    public class NamedPipesTest
    {
        private const string PIPE_NAME = "Clipbox_TEST_PIPE";

        private const int WAIT_TIMEOUT_MS = 15000;
        
        [NonParallelizable]
        [TestCase("message1", "message1", "response1", "response1", true)]
        [TestCase("message1", "message2", "response1", "response2", false)]
        public void TestSingleClient(string sentMessage, string expectedMessage, string expectedServerResponse, string serverResponse, bool shouldSucceed)
        {
            bool recieveSuccess = false;
            Exception exception = null;
            bool clientSuccess = false;
            bool responseSuccess = false;
            
            using (ManualResetEvent mre = new ManualResetEvent(false))
            using (NamedPipesServer server = new NamedPipesServer(PIPE_NAME))
            {
                server.GotMessage += (sender, args) =>
                {
                    recieveSuccess = args.ReceivedMessage == expectedMessage;
                    args.Response = serverResponse;
                    
                    mre.Set();
                };

                server.ErrorOccurred += (sender, args) =>
                {
                    exception = args.Exception;
                    mre.Set();
                };

                NamedPipesClient client = new NamedPipesClient(PIPE_NAME);
                clientSuccess = client.SendMessage(sentMessage, out string answer);
                
                responseSuccess = answer == expectedServerResponse;

                mre.WaitOne(WAIT_TIMEOUT_MS);
            }
            
            Assert.IsTrue(shouldSucceed == recieveSuccess);
            Assert.IsTrue(shouldSucceed == responseSuccess);
            Assert.IsNull(exception);
        }

        [NonParallelizable]
        [TestCase("message1", "message1", true, 64)]
        [TestCase("message1", "message2", false, 64)]
        public void TestMultipleParrallelClients(string sentMessage, string expectedMessage, bool shouldSucceed, int numberOfClients)
        {
            bool recieveSuccess = false;
            bool anyConnectionFailed = false;
            Exception exception = null;
            
            using (ManualResetEvent mre = new ManualResetEvent(false))
            using (NamedPipesServer server = new NamedPipesServer(PIPE_NAME))
            {
                server.GotMessage += (sender, args) =>
                {
                    if (shouldSucceed)
                    {
                        recieveSuccess = args.ReceivedMessage == expectedMessage;
                    }
                    else
                    {
                        recieveSuccess = args.ReceivedMessage != expectedMessage;
                    }

                    if (!recieveSuccess)
                    {
                        anyConnectionFailed = true;
                    }

                    mre.Set();
                };

                server.ErrorOccurred += (sender, args) =>
                {
                    exception = args.Exception;
                    mre.Set();
                };

                TestUtils.RunMultipleTimesInParralel(() =>
                {
                    NamedPipesClient client = new NamedPipesClient(PIPE_NAME);
                    client.SendMessage(sentMessage);
                }, numberOfClients);
                    
                mre.WaitOne(WAIT_TIMEOUT_MS);
            }

            Assert.IsTrue(recieveSuccess);
            Assert.IsFalse(anyConnectionFailed);
            Assert.IsNull(exception);
        }
    }
}
