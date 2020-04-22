using Clipbox.Infrastructure.Persistence.FilesystemAccess;
using Clipbox.Infrastructure.Testing;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace Clipbox.Infrastructure.Tests.Persistence
{
    [TestFixture]
    public class AppDataUtilsTests
    {
        private string _tempDirectoryPath;

        private string _tempFileName;

        private const string _testContent = "test";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Create a temp test directory
            _tempDirectoryPath = Path.Combine(Path.GetTempPath(), nameof(AppDataUtilsTests));
            Directory.CreateDirectory(_tempDirectoryPath);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // do not delete AppDataUtils.MainAppDataDirectory, as it may turn out to
            // be %appdata% instead of the test temp directory, due to error in the tests
            // or in the AppDataUtils class.
            Directory.Delete(_tempDirectoryPath, true);
        }

        [SetUp]
        public void SetUp()
        {
            _tempFileName = Guid.NewGuid().ToString();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AppDataUtils_WriteToApplicationData_ShouldSucceed(bool useLock)
        {
            AppDataUtils.WriteToApplicationData(_tempFileName, _testContent, useLock);
            string readContent = AppDataUtils.ReadTextFromApplicationData(_tempFileName, useLock);

            Assert.AreEqual(_testContent, readContent);
        }

        [TestCase(64)]
        public void AppDataUtils_WriteToApplicationData_Parallel_ShouldSucceed(int numberOfThreads)
        {
            string readContent = null;

            TestUtils.RunMultipleTimesInParralel(() =>
            {
                AppDataUtils.WriteToApplicationData(_tempFileName, _testContent);
                readContent = AppDataUtils.ReadTextFromApplicationData(_tempFileName);
            }, numberOfThreads);
            
            Assert.AreEqual(_testContent, readContent);
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void AppDataUtils_WriteToApplicationData_Append_ShouldSucceed(bool append, bool useLock)
        {
            AppDataUtils.WriteToApplicationData(_tempFileName, _testContent, append: append, useLock: useLock);
            AppDataUtils.WriteToApplicationData(_tempFileName, _testContent, append: append, useLock: useLock);
            string readContent = AppDataUtils.ReadTextFromApplicationData(_tempFileName, useLock);

            Assert.AreEqual(append ? string.Concat(_testContent, _testContent) : _testContent, readContent);
        }
        
        [TestCase(true)]
        [TestCase(false)]
        public void AppDataUtils_WriteBytesToApplicationData_ShouldSucceed(bool useLock)
        {
            AppDataUtils.WriteToApplicationData(_tempFileName, Encoding.UTF8.GetBytes(_testContent), useLock);
            byte[] readContent = AppDataUtils.ReadBytesFromApplicationData(_tempFileName, useLock);

            Assert.AreEqual(_testContent, Encoding.UTF8.GetString(readContent));
        }

        [TestCase(64)]
        public void AppDataUtils_WriteBytesToApplicationData_Parallel_ShouldSucceed(int numberOfThreads)
        {
            byte[] readContent = null;
            TestUtils.RunMultipleTimesInParralel(() =>
            {
                AppDataUtils.WriteToApplicationData(_tempFileName, Encoding.UTF8.GetBytes(_testContent));
                readContent = AppDataUtils.ReadBytesFromApplicationData(_tempFileName);
            }, numberOfThreads);

            Assert.AreEqual(_testContent, Encoding.UTF8.GetString(readContent));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AppDataUtils_ReadTextFromApplicationData_FileNotFound_ShouldFail(bool useLock)
        {
            TestDelegate testAction = () =>
            {
                AppDataUtils.ReadTextFromApplicationData(_tempFileName, useLock);
            };

            Assert.Throws<FileNotFoundException>(testAction);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AppDataUtils_ReadBytesFromApplicationData_FileNotFound_ShouldFail(bool useLock)
        {
            TestDelegate testAction = () =>
            {
                AppDataUtils.ReadBytesFromApplicationData(_tempFileName, useLock);
            };

            Assert.Throws<FileNotFoundException>(testAction);
        }
    }
}
