using Clipbox.Core.ClipboardContent.Interfaces;
using System;
using System.Collections.Generic;

namespace Clipbox.Core.ClipboardContent
{
    [Serializable]
    public class PreviewDimensionInfo : IHaveMonitorInfo
    {
        // monitor info
        public int WorkAreaWidth { get; set; }
        public int WorkAreaHeight { get; set; }
        public double DPI { get; set; }

        public IEnumerable<IPreviewDimensions> PreviewDimentions { get; set; }

        public bool IsViewItemGroup { get; set; }
    }
}
