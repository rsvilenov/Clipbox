using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.IndexUnits
{
    public class SourceWindowCaptionIndexUnit : IndexUnitBase<string>
    {
        public SourceWindowCaptionIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

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
