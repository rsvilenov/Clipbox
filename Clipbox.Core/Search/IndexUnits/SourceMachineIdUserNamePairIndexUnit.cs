using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.IndexUnits
{
    [Serializable]
    public class SourceMachineIdUserNamePairIndexUnit : IndexUnitBase<string>
    {
        public SourceMachineIdUserNamePairIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected SourceMachineIdUserNamePairIndexUnit(SerializationInfo info, StreamingContext context)
           : base(info, context) { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineIdUserNamePair;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                $"{clipboardItem.Source.SourceSessionInfo.UserName}@{clipboardItem.Source.SourceMachineInfo.MachineUniqueId}"
            };
        }
    }
}
