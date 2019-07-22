using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class SerializablePreviewDimensions : IPreviewDimensions
    {
        public int PreviewWidth { get; set; }
        public int PreviewHeight { get; set; }
    }
}
