using Clipbox.Infrastructure.UI.DataObjects;
using System;
using System.Collections.Generic;

namespace Clipbox.Services.DataObjects
{
    [Serializable]
    public class PreviewDimensionInfo
    {
        public MonitorDimensions MonitorDimensions { get; set; }

        public List<PreviewDimensions> PreviewDimensions { get; set; }

        public bool ArePreviewDimensionsDpiScaled { get; set; }

        public bool IsViewItemGroup { get; set; }

        public int NumberOfItemsWithoutPreview { get; set; }
    }
}
