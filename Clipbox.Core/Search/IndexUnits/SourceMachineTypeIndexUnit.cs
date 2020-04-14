using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.IndexUnits
{
    public class SourceMachineTypeIndexUnit : IndexUnitBase<MachineType>
    {
        public SourceMachineTypeIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineType;

        protected override IEnumerable<MachineType> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new MachineType[] { clipboardItem.Source.SourceMachineInfo.MachineType };
        }
    }
}
