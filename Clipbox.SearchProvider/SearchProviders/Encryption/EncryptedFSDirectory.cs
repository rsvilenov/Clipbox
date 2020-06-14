using Clipbox.Infrastructure.Encryption;
using Lucene.Net.Store;
using System.IO;

namespace Clipbox.Infrastructure.Search.SearchProviders.Encryption
{
    public class EncryptedFSDirectory : FSDirectory
    {
        private readonly DirectoryInfo _dirInfo;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly LockFactory _lockFactory;

        public EncryptedFSDirectory(DirectoryInfo path, IEncryptionProvider encryptionProvider, LockFactory lockFactory) : base(path, lockFactory)
        {
            _dirInfo = path;
            _encryptionProvider = encryptionProvider;
            _lockFactory = lockFactory;
        }

        public override IndexOutput CreateOutput(string name)
        {
            return new EncrypedIndexOutput(Path.Combine(_dirInfo.FullName, name), _encryptionProvider);
        }

        public override IndexInput OpenInput(string name, int bufferSize)
        {
            return new EncryptedIndexInput(Path.Combine(_dirInfo.FullName, name), _encryptionProvider, bufferSize);
        }

        public override long FileLength(string name)
        {
            using (var eii = new EncryptedIndexInput(Path.Combine(_dirInfo.FullName, name), _encryptionProvider))
            {
                return eii.Length();
            }
        }
    }
}
