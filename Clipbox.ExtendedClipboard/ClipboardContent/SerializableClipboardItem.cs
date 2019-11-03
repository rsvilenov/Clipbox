using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal abstract class SerializableClipboardItem : IHaveClipboardItemInfo
    {
        #region IHaveClipboardItemInfo members

        public IClipboardItemSource Source { get; set; }

        public string Id { get; set; }
        
        public object Tag { get; set; }

        #endregion

        public bool IsPinned { get; set; }

        public ITimestampInfo TimestampInfo { get; set; }
    }
}
