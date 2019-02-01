using System;
using System.Collections.Generic;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class PersistableClipboardItem : SerializableClipboardItem
    {
        public IEnumerable<PersistableClipboardDataObject> PersistableClipboardDataObjects { get; set; }
    }
}
