using Clipbox.Infrastructure.UI.DataObjects;
using System;
using System.Collections.Generic;

namespace Clipbox.Services.DataObjects
{
    [Serializable]
    public class PreviewDimensionInfo
    {
        public MonitorDimensions MonitorDimensions { get; set; }

        public List<PreviewDimensions> PreviewDimentions { get; set; }

        public bool IsViewItemGroup { get; set; }
    }
}
