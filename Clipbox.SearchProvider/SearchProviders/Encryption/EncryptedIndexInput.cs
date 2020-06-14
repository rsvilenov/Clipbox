using Clipbox.Infrastructure.Encryption;
using Lucene.Net.Store;
using System.IO;

namespace Clipbox.Infrastructure.Search.SearchProviders.Encryption
{
    internal class EncryptedIndexInput : BufferedIndexInput
    {
        private MemoryStream _memoryStream;

        private bool _isClone;

        private bool _isDisposed;

        public EncryptedIndexInput(string filePath, IEncryptionProvider encryptionProvider, int bufferSize)
            : base(bufferSize)
        {
            Init(filePath, encryptionProvider);
        }

        public EncryptedIndexInput(string filePath, IEncryptionProvider encryptionProvider)
        {
            Init(filePath, encryptionProvider);
        }

        private void Init(string filePath, IEncryptionProvider encryptionProvider)
        {
            if (File.Exists(filePath))
            {
                byte[] bytesToDecrypt = File.ReadAllBytes(filePath);
                byte[] decryptedBytes = encryptionProvider.Decrypt(bytesToDecrypt);

                _memoryStream = new MemoryStream(decryptedBytes);
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
                _isDisposed = true;
            }
        }

        public override void ReadInternal(byte[] b, int offset, int length)
        {
            if (FilePointer != _memoryStream.Position)
            {
                _memoryStream.Seek(FilePointer, SeekOrigin.Begin);
            }

            _memoryStream.Read(b, offset, length);
        }

        public override void SeekInternal(long pos)
        {
            _memoryStream.Seek(pos, SeekOrigin.Begin);
        }
    }
}
