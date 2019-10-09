using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceMachineNameSearchTerm : SearchTermBase<string>
    {
        public SourceMachineNameSearchTerm(IEnumerable<string> sourceMachineNames)
            : base(sourceMachineNames)
        { }

        public SourceMachineNameSearchTerm(IClipboardItem clipboardItem)
            : base(clipboardItem)
        { }

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                clipboardItem.Source.MachineName
            };
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.SourceMachineName;
    }
}
