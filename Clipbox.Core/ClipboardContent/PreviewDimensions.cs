using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.Core.ClipboardContent
{
    [Serializable]
    public class PreviewDimensions : IPreviewDimensions
    {
        public int PreviewWidth { get; set; }
        public int PreviewHeight { get; set; }
    }
}
