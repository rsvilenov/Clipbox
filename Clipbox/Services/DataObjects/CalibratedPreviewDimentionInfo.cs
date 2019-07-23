using System.Collections.Generic;

namespace Clipbox.Services.DataObjects
{
    public class CalibratedPreviewDimentionInfo
    {
        public IEnumerable<PreviewDimensions> PreviewDimentions { get; set; }

        public bool IsViewItemGroup { get; set; }
    }
}
