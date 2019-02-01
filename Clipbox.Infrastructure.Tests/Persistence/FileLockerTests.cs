using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Testing;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.Tests.Persistence
{
    [TestFixture]
    public class FileLockerTests
    {
        private string _testFileName;

        [SetUp]
        public void SetUp()
        {
            _testFileName = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete(_testFileName);
        }

        [Test]
        public void FileLocker_ShouldSucceed()
        {
            string testContent = "abc";
            string readContent = null;

            using (FileLocker locker = new FileLocker(_testFileName))
            {
                File.WriteAllText(_testFileName, testContent);
            }

            readContent = File.ReadAllText(_testFileName);


            Assert.AreEqual(readContent, testContent);
        }

        [Test]
        public void FileLocker_ShouldFail_Timeout()
        {
            int timeoutMs = 100;

            Assert.Throws<TimeoutException>(() =>
            {
                // lock the file
                using (FileLocker locker = new FileLocker(_testFileName))
                {
                    // then try to lock it again with a timeout
                    using (FileLocker innerLocker = new FileLocker(_testFileName, timeoutMs))
                    {
                    }
                }
            });
        }

        [TestCase(1000)]
        public void FileLocker_NestedLocksTimeout_ShouldFail(int secondLockTimeout)
        {
            TestDelegate testAction = () =>
            {
                // lock the file
                using (FileLocker locker = new FileLocker(_testFileName))
                {
                    // then try to lock it again with a timeout
                    using (FileLocker innerLocker = new FileLocker(_testFileName, secondLockTimeout))
                    {
                    }
                }
                
            };
            
            Assert.Throws<TimeoutException>(testAction);
        }

        [TestCase(2, 3000)]
        public void FileLocker_SubsequentLocksLifespan_ShouldSucceed(int timespanSeconds, int testWaitTimeoutMs)
        {
            bool secondLockSucceeded = false;

            var cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                // lock the file
                using (FileLocker locker = new FileLocker(_testFileName,
                    0,
                    TimeSpan.FromSeconds(timespanSeconds),
                    // do not delete the lock file after Dispose(), so that
                    // a case where the process exists abruptly is simulated.
                    leaveLockFileOnDispose: true))
                {
                }

                // then try to wait for the lock above to expire and lock the file again
                using (FileLocker innerLocker = new FileLocker(_testFileName))
                {
                    secondLockSucceeded = true;
                }

            }).Wait(testWaitTimeoutMs, cts.Token);

            Assert.IsTrue(secondLockSucceeded);
        }

        [TestCase(1000)]
        public void FileLocker_NestedLocksNoTimeout_ShouldFail(int lockWaitTimeoutMs)
        {
            bool secondLockEntered = false;

            var cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                // lock the file
                using (FileLocker locker = new FileLocker(_testFileName))
                {
                    // then try to lock it again with a timeout
                    using (FileLocker innerLocker = new FileLocker(_testFileName))
                    {
                        secondLockEntered = true;
                    }
                }

            }).Wait(lockWaitTimeoutMs, cts.Token);

            Assert.IsFalse(secondLockEntered);
        }

        /// <summary>
        /// Multithreaded test with the maximum number of WaitHandles allowed (64).
        /// </summary>
        /// <param name="numberOfThreads"></param>
        [TestCase(64)]
        public void FileLocker_ShouldSucceed_Parallel(int numberOfThreads)
        {
            bool success = true;

            const string textToWrite = "some text";

            TestUtils.RunMultipleTimesInParralel(() =>
            {
                try
                {
                    using (FileLocker innerLocker = new FileLocker(_testFileName))
                    {
                        File.WriteAllText(_testFileName, textToWrite);
                    }
                }
                catch (Exception)
                {
                    success = false;
                }
            }, numberOfThreads);
            
            Assert.IsTrue(success);
            Assert.AreEqual(File.ReadAllText(_testFileName), textToWrite);
        }
    }
}
