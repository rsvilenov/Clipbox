using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.IndexUnits
{
    public class SourceMachineIdUserNamePairIndexUnit : IndexUnitBase<string>
    {
        public SourceMachineIdUserNamePairIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineIdUserNamePair;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                $"{clipboardItem.Source.UserName}@{clipboardItem.Source.SourceMachineInfo.MachineUniqueId}"
            };
        }
    }
}
