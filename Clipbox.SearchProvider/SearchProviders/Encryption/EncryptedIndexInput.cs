using Clipbox.Infrastructure.Encryption;
using Lucene.Net.Store;
using System.IO;
using System.Security.Cryptography;

namespace Clipbox.Infrastructure.Search.SearchProviders.Encryption
{
    internal class EncryptedIndexInput : BufferedIndexInput
    {
        private readonly MemoryStream _memoryStream;
        private readonly AesEncryption _aes;

        private bool _isClone;

        private bool _isDisposed;

        public override long FilePointer => _memoryStream.Position;

        public EncryptedIndexInput(string filePath, string key)
        {
            _aes = new AesEncryption(key);

            if (File.Exists(filePath))
            {
                byte[] bytesToDecrypt = File.ReadAllBytes(filePath);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, _aes.Decryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToDecrypt, 0, bytesToDecrypt.Length);
                    }

                    byte[] decryptedBytes = ms.ToArray();
                    _memoryStream = new MemoryStream(decryptedBytes);
                }
            }
            else
            {
                _memoryStream = new MemoryStream();
            }
        }

        public override long Length()
        {
            return _memoryStream.ToArray().Length;
        }
        
        public override object Clone()
        {
            EncryptedIndexInput clone = (EncryptedIndexInput)base.Clone();
            clone._isClone = true;
            return clone;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && !_isClone)
            {
                _memoryStream.Dispose();
                _aes.Dispose();
                _isDisposed = true;
            }
        }

        public override void ReadInternal(byte[] b, int offset, int length)
        {
            _memoryStream.Read(b, offset, length);
        }

        public override void SeekInternal(long pos)
        {
            _memoryStream.Seek(pos, SeekOrigin.Begin);
        }
    }
}
