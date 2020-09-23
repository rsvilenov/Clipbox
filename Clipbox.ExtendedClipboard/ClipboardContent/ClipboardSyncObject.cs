using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using System.Diagnostics;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [DebuggerDisplay("{Action} - {Id}")]
    internal class ClipboardSyncObject : IClipboardSyncObject
    {
        public string Id { get; set; }

        public byte[] Data { get; set; }
        
        public long ModifiedDateUtcTicks { get; set; }

        public SyncAction Action { get; set; }
    }
    
}
