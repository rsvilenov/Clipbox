using Clipbox.Core.ClipboardContent.Interfaces;
using System;
using System.Collections.Generic;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class PersistableClipboardItem : SerializableClipboardItem, IHaveTransferabilityInfo
    {
        public IEnumerable<PersistableClipboardDataObject> PersistableClipboardDataObjects { get; set; }
        
        public bool CanTransfer { get; set; }

        public bool ShouldTransfer { get; set; }
    }
}
