using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.IndexUnits
{
    public class SourceMachineUniqueIdIndexUnit : IndexUnitBase<string>
    {
        public SourceMachineUniqueIdIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineUniqueId;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                clipboardItem.Source.SourceMachineInfo.MachineUniqueId
            };
        }
    }
}
