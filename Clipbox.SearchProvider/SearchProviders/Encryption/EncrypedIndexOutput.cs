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
        private readonly AesEncryption _aes;

        private bool _hasUnflashedData;

        public EncrypedIndexOutput(string filePath, string key)
        {
            _filePath = filePath;
            _aes = new AesEncryption(key);
            _memoryStream = new MemoryStream();
            
        }

        public override long Length => _memoryStream.Length;

        public override long FilePointer => _memoryStream.Position;

        public override void Flush()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, _aes.Encryptor, CryptoStreamMode.Write))
                {
                    byte[] bytesToWrite = _memoryStream.ToArray();
                    cs.Write(bytesToWrite, 0, bytesToWrite.Length);
                }

                byte[] encryptedBytes = ms.ToArray();
                File.WriteAllBytes(_filePath, encryptedBytes);
            }
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
            _aes.Dispose();
        }
    }
}
