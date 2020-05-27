using Lucene.Net.Store;
using System.IO;

namespace Clipbox.Infrastructure.Search.SearchProviders.Encryption
{
    public class EncryptedFSDirectory : FSDirectory
    {
        private readonly DirectoryInfo _dirInfo;
        public EncryptedFSDirectory(DirectoryInfo path, LockFactory lockFactory) : base(path, lockFactory)
        {
            _dirInfo = path;
        }

        public override IndexOutput CreateOutput(string name)
        {
            return new EncrypedIndexOutput(Path.Combine(_dirInfo.FullName, name), "test");
        }

        public override IndexInput OpenInput(string name, int bufferSize)
        {
            return new EncryptedIndexInput(Path.Combine(_dirInfo.FullName, name), "test");
        }

        public override long FileLength(string name)
        {
            using (var eii = new EncryptedIndexInput(Path.Combine(_dirInfo.FullName, name), "test"))
            {
                return eii.Length();
            }
        }
        
 
    }

}
