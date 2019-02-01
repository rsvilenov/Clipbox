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
        [TestCase("message1", "message1", true)]
        [TestCase("message1", "message2", false)]
        public void TestSingleClient(string sentMessage, string expectedMessage, bool shouldSucceed)
        {
            bool recieveSuccess = false;
            Exception exception = null;
            
            using (ManualResetEvent mre = new ManualResetEvent(false))
            using (NamedPipesServer server = new NamedPipesServer(PIPE_NAME))
            {
                server.GotMessage += (sender, args) =>
                {
                    if (shouldSucceed)
                    {
                        recieveSuccess = args.Response == expectedMessage;
                    }
                    else
                    {
                        recieveSuccess = args.Response != expectedMessage;
                    }

                    mre.Set();
                };

                server.ErrorOccurred += (sender, args) =>
                {
                    exception = args.Exception;
                    mre.Set();
                };

                NamedPipesClient client = new NamedPipesClient(PIPE_NAME);
                client.SendMessage(sentMessage);

                mre.WaitOne(WAIT_TIMEOUT_MS);
            }
            
            Assert.IsTrue(recieveSuccess);
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
                        recieveSuccess = args.Response == expectedMessage;
                    }
                    else
                    {
                        recieveSuccess = args.Response != expectedMessage;
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
