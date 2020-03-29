using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.IndexUnits
{
    public class CreationDateIndexUnit : IndexUnitBase<long>
    {
        public CreationDateIndexUnit(IClipboardItem clipboardItem)
                : base(clipboardItem) { }

        protected override SearchCategory SearchCategory => SearchCategory.CreationDate;

        protected override IEnumerable<long> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            yield return clipboardItem.TimestampInfo.EntryDateUtcTicks;
        }
    }
}