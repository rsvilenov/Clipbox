using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.IndexUnits
{
    [Serializable]
    public class TagIndexUnit : IndexUnitBase<string>
    {
        public TagIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected TagIndexUnit(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        protected override SearchCategory SearchCategory => SearchCategory.Tag;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                clipboardItem.Tag?.ToString() ?? string.Empty
            };
        }
    }
}
