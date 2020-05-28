using Clipbox.Infrastructure.Encryption.EncryptionProviders;
using Clipbox.Infrastructure.Persistence;
using NUnit.Framework;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.Encryption.Tests
{
    [TestFixture]
    public class AesEncryptionProviderTests
    {
        private IPersistenceProvider _mockPersistenceProvider;
        private AesEncryptionProvider _shaEncryptionProvider;

        [SetUp]
        public void SetUp()
        {
            _mockPersistenceProvider = MockRepository.GenerateMock<IPersistenceProvider>();

            byte[] generatedRandomKey = null;

            _mockPersistenceProvider.Stub(pp => pp.Write(Arg<string>.Is.Anything, Arg<byte[]>.Is.Anything, Arg<string>.Is.Anything))
                .WhenCalled(w => generatedRandomKey = (byte[])w.Arguments[1]);

            _mockPersistenceProvider.Stub(pp => pp.ReadBytes(Arg<string>.Is.Anything, Arg<string>.Is.Anything))
                .WhenCalled(w => w.ReturnValue = generatedRandomKey)
                .Return(generatedRandomKey);

            _shaEncryptionProvider = new AesEncryptionProvider(_mockPersistenceProvider);
        }

        [Test]
        public void TestEncryptionWithPassword()
        {
            byte[] bytesToEncrypt = { 0, 1, 2, 5, 6, 7, 8, 3, 4, 6, 7, 8 };
            string password = "123";

            byte[] encrypted = _shaEncryptionProvider.Encrypt(bytesToEncrypt, password);
            byte[] decrypted = _shaEncryptionProvider.Decrypt(encrypted, password);

            Assert.AreEqual(bytesToEncrypt, decrypted);
        }
        

        [Test]
        public void TestEncryptionWithGeneratedKey()
        {
            byte[] bytesToEncrypt = { 0, 1, 2, 5, 6, 7, 8, 3, 4, 6, 7, 8 };


            byte[] encrypted = _shaEncryptionProvider.Encrypt(bytesToEncrypt);
            byte[] decrypted = _shaEncryptionProvider.Decrypt(encrypted);

            Assert.AreEqual(bytesToEncrypt, decrypted);

        }
    }
}
