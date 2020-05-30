using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.IndexUnits
{
    [Serializable]
    public class SourceMachineNameIndexUnit : IndexUnitBase<string>
    {
        public SourceMachineNameIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected SourceMachineNameIndexUnit(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineName;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                clipboardItem.Source.SourceMachineInfo.MachineName
            };
        }
    }
}
