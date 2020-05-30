using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.IndexUnits
{
    [Serializable]
    public class CreationDateIndexUnit : IndexUnitBase<long>
    {
        public CreationDateIndexUnit(IClipboardItem clipboardItem)
                : base(clipboardItem) { }

        protected CreationDateIndexUnit(SerializationInfo info, StreamingContext context)
           : base(info, context) { }

        protected override SearchCategory SearchCategory => SearchCategory.CreationDate;

        protected override IEnumerable<long> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            yield return clipboardItem.TimestampInfo.EntryDateUtcTicks;
        }
    }
}