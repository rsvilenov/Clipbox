using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal abstract class SerializableClipboardItem : IHaveClipboardItemInfo, IHavePreviewDimentions
    {
        #region IHaveClipboardItemInfo members

        public IClipboardItemSource Source { get; set; }

        public string Id { get; set; }
        
        public object Tag { get; set; }

        public long EntryDateTicks { get; set; }

        public long EntryDateUtcTicks { get; set; }
        
        public long ModifiedDateTicks { get; set; }

        public long ModifiedDateUtcTicks { get; set; }

        #endregion

        public bool IsPinned { get; set; }

        public IPreviewDimentions PreviewDimentions { get; set; }
    }
}
