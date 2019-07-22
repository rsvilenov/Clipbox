using Clipbox.Core.ClipboardContent.Interfaces;
using System;
using System.Collections.Generic;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class SerializablePreviewDimensionInfo : IHaveMonitorInfo
    {
        // monitor info
        public int WorkAreaWidth { get; set; }
        public int WorkAreaHeight { get; set; }
        public double DPI { get; set; }

        public IEnumerable<SerializablePreviewDimensions> PreviewDimentions { get; set; }

        public bool IsViewItemGroup { get; set; }
    }
}
