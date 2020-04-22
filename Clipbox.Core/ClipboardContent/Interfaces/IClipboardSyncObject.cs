using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IClipboardSyncObject
    {
        string Id { get; set; }

        byte[] Data { get; set; }

        long ModifiedDateUtcTicks { get; set; }

        SyncAction Action { get; set; }

        SyncStatus Status { get; set; }
    }
}
