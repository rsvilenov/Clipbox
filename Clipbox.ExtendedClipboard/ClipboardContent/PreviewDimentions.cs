using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    public class PreviewDimentions : IPreviewDimentions
    {
        // monitor info
        public int MonitorWidth { get; internal set; }
        public int MonitorHeight { get; internal set; }
        public int MonitorDPI { get; internal set; }

        // item preview info
        public int PreviewWidth { get; internal set; }
        public int PreviewHeight { get; internal set; }
    }
}
