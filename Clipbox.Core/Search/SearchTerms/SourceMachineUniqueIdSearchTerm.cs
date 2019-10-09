using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceMachineUniqueIdSearchTerm : SearchTermBase<string>
    {
        public SourceMachineUniqueIdSearchTerm(IEnumerable<string> sourceMachineUniqueIds)
            : base(sourceMachineUniqueIds)
        { }

        public SourceMachineUniqueIdSearchTerm(IClipboardItem clipboardItem)
            : base(clipboardItem)
        { }

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                clipboardItem.Source.MachineUniqueId
            };
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.SourceMachineUniqueId;
    }
}
