using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IClipboardSyncObject
    {
        string Id { get; }

        byte[] Data { get; }

        long ModifiedDateUtcTicks { get; }

        SyncAction Action { get; }
    }
}
