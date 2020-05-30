using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.IndexUnits
{
    [Serializable]
    public class SourceMachineTypeIndexUnit : IndexUnitBase<MachineType>
    {
        public SourceMachineTypeIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected SourceMachineTypeIndexUnit(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineType;

        protected override IEnumerable<MachineType> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new MachineType[] { clipboardItem.Source.SourceMachineInfo.MachineType };
        }
    }
}
