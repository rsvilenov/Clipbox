using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.IndexUnits
{
    [Serializable]
    public class SourceMachineUniqueIdIndexUnit : IndexUnitBase<string>
    {
        public SourceMachineUniqueIdIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected SourceMachineUniqueIdIndexUnit(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

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
