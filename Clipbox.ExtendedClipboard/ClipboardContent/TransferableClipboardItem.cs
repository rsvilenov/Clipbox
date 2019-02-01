using System;
using System.Collections.Generic;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class TransferableClipboardItem : SerializableClipboardItem
    {
        public IEnumerable<TransferableClipboardDataObject> TransferableClipboardDataObjects { get; set; }
    }
}
