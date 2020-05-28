using Clipbox.Infrastructure.Encryption;
using Lucene.Net.Store;
using System.IO;
using System.Security.Cryptography;

namespace Clipbox.Infrastructure.Search.SearchProviders.Encryption
{
    internal class EncrypedIndexOutput : IndexOutput
    {
        private readonly string _filePath;
        private readonly MemoryStream _memoryStream;
        private readonly IEncryptionProvider _encryptionProvider;

        private bool _hasUnflashedData;

        public EncrypedIndexOutput(string filePath, IEncryptionProvider encryptionProvider)
        {
            _filePath = filePath;
            _encryptionProvider = encryptionProvider;
            _memoryStream = new MemoryStream();
            
        }

        public override long Length => _memoryStream.Length;

        public override long FilePointer => _memoryStream.Position;

        public override void Flush()
        {
            byte[] bytesToWrite = _memoryStream.ToArray();

            byte[] encryptedBytes = _encryptionProvider.Encrypt(bytesToWrite);
            File.WriteAllBytes(_filePath, encryptedBytes);

            _hasUnflashedData = false;
        }

        public override void Seek(long pos)
        {
            _memoryStream.Seek(pos, SeekOrigin.Begin);
        }

        public override void WriteByte(byte b)
        {
            _memoryStream.WriteByte(b);
            _hasUnflashedData = true;
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            _memoryStream.Write(b, offset, length);
            _hasUnflashedData = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (_hasUnflashedData)
            {
                Flush();
            }
            _memoryStream.Dispose();
        }
    }
}
