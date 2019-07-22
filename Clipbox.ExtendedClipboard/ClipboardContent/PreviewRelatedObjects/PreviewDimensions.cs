using Clipbox.Core.ClipboardContent.Interfaces;

namespace Clipbox.ExtendedClipboard.ClipboardContent.PreviewRelatedObjects
{
    public class SerializablePreviewDimensions : IPreviewDimensions
    {
        public int PreviewWidth { get; set; }
        public int PreviewHeight { get; set; }
    }
}
