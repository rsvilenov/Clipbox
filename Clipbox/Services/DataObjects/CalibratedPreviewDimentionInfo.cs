using System.Collections.Generic;

namespace Clipbox.Services.DataObjects
{
    public class CalibratedPreviewDimentionInfo
    {
        public IEnumerable<PreviewDimensions> PreviewDimentions { get; set; }

        public bool IsViewItemGroup { get; set; }

        /// <summary>
        /// Number of items without previews. Used only when IsViewItemGroup is true.
        /// </summary>
        public int NumberOfItemsWithoutPreview { get; set; }
    }
}
