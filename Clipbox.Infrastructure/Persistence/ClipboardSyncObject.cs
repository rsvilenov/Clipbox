using System.Diagnostics;

namespace Clipbox.Infrastructure.Persistence
{
    [DebuggerDisplay("{Action} - {Status} - {Id}")]
    public class ClipboardSyncObject
    {
        public string Id { get; set; }

        public byte[] Data { get; set; }
        
        public long ModifiedDateUtcTicks { get; set; }

        public SyncAction Action { get; set; }

        public SyncStatus Status { get; set; }

        public enum SyncStatus
        {
            Pending,
            Executed
        }

        public enum SyncAction
        {
            Create,
            Update,
            Delete
        }
    }
    
}
