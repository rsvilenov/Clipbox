using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.IndexUnits
{
    [Serializable]
    public class SourceWindowCaptionIndexUnit : IndexUnitBase<string>
    {
        public SourceWindowCaptionIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected SourceWindowCaptionIndexUnit(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceWindowName;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                clipboardItem.Source.SourceWindowInfo.Caption
            };
        }
    }
}
