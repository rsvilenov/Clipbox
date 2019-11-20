using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Search.IndexUnits
{
    public class ClipboardContentTypeIndexUnit : IndexUnitBase<ClipboardContentClass>
    {
        public ClipboardContentTypeIndexUnit(IClipboardItem clipboardItem)
            : base (clipboardItem) { }

        protected override SearchCategory SearchCategory => SearchCategory.Type;

        protected override IEnumerable<ClipboardContentClass> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return clipboardItem
                .ClipboardDataObjects
                .Select(cdo => cdo.IntermediateFormatMetadata.ContentClass)
                .Distinct();
        }
    }
}
