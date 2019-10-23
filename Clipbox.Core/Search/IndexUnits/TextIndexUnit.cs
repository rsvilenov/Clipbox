using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.IndexUnits
{
    public class TextIndexUnit : IndexUnitBase<string>
    {
        /// <summary>
        /// This search index unit can be constructed only with a string collection
        /// as the values of the strings cannot be infered from the IClipboardItem,
        /// and the SearchSupportExtensions are used for that.
        /// </summary>
        public TextIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        public TextIndexUnit(IEnumerable<string> values)
            : base(values) { }

        protected override SearchCategory SearchCategory => SearchCategory.Text;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            // we will construct this class only with ready strings, 
            // so we will not need this method
            throw new System.NotImplementedException();
        }
    }
}
