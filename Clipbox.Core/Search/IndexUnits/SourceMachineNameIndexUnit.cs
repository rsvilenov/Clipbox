using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.IndexUnits
{
    public class SourceMachineNameIndexUnit : IndexUnitBase<string>
    {
        public SourceMachineNameIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineName;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                clipboardItem.Source.MachineName
            };
        }
    }
}
