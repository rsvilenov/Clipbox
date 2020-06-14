using Clipbox.Infrastructure.Encryption;
using Lucene.Net.Store;
using System.IO;

namespace Clipbox.Infrastructure.Search.SearchProviders.Encryption
{
    internal class EncrypedIndexOutput : BufferedIndexOutput
    {
        private readonly string _filePath;
        private readonly MemoryStream _memoryStream;
        private readonly IEncryptionProvider _encryptionProvider;

        private bool _isOpen;

        public EncrypedIndexOutput(string filePath, IEncryptionProvider encryptionProvider)
        {
            _filePath = filePath;
            _encryptionProvider = encryptionProvider;
            _memoryStream = new MemoryStream();
            _isOpen = true;
        }

        public override void FlushBuffer(byte[] b, int offset, int len)
        {
            _memoryStream.Write(b, offset, len);

            byte[] bytesToWrite = _memoryStream.ToArray();

            byte[] encryptedBytes = _encryptionProvider.Encrypt(bytesToWrite);
            File.WriteAllBytes(_filePath, encryptedBytes);
        }

        public override void Seek(long pos)
        {
            base.Seek(pos);
            _memoryStream.Seek(pos, System.IO.SeekOrigin.Begin);
        }

        public override long Length
        {
            get { return _memoryStream.Length; }
        }

        public override void SetLength(long length)
        {
            base.SetLength(length);
            _memoryStream.SetLength(length);
        }

        protected override void Dispose(bool disposing)
        {
            if (_isOpen)
            {
                try
                {
                    base.Dispose(disposing);
                }
                finally
                {
                    _isOpen = false;
                    _memoryStream.Dispose();
                }
            }
        }
    }

    //internal class EncrypedIndexOutput : IndexOutput
    //{
    //    private readonly string _filePath;
    //    private readonly MemoryStream _memoryStream;
    //    private readonly IEncryptionProvider _encryptionProvider;

    //    private bool _hasUnflashedData;

    //    public EncrypedIndexOutput(string filePath, IEncryptionProvider encryptionProvider)
    //    {
    //        _filePath = filePath;
    //        _encryptionProvider = encryptionProvider;
    //        _memoryStream = new MemoryStream();

    //    }

    //    public override long Length => _memoryStream.Length;

    //    public override long FilePointer => _memoryStream.Position;

    //    public override void Flush()
    //    {
    //        byte[] bytesToWrite = _memoryStream.ToArray();

    //        byte[] encryptedBytes = _encryptionProvider.Encrypt(bytesToWrite);
    //        File.WriteAllBytes(_filePath, encryptedBytes);

    //        _hasUnflashedData = false;
    //    }

    //    public override void Seek(long pos)
    //    {
    //        _memoryStream.Seek(pos, SeekOrigin.Begin);
    //    }

    //    public override void WriteByte(byte b)
    //    {
    //        _memoryStream.WriteByte(b);
    //        _hasUnflashedData = true;
    //    }

    //    public override void WriteBytes(byte[] b, int offset, int length)
    //    {
    //        _memoryStream.Write(b, offset, length);
    //        _hasUnflashedData = true;
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        if (_hasUnflashedData)
    //        {
    //            Flush();
    //        }
    //        _memoryStream.Dispose();
    //    }
    //}
}
