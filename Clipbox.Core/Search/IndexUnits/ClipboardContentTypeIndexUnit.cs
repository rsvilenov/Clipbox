using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.IndexUnits
{
    [Serializable]
    public class ClipboardContentTypeIndexUnit : IndexUnitBase<ClipboardContentClass>
    {
        public ClipboardContentTypeIndexUnit(IClipboardItem clipboardItem)
            : base (clipboardItem) { }

        protected ClipboardContentTypeIndexUnit(SerializationInfo info, StreamingContext context)
           : base(info, context) { }

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
