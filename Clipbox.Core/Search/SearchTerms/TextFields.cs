using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipbox.Core.Search.Enums;

namespace Clipbox.Core.Search.SearchTerms
{
    public class TextFields : SearchTermBase<string>
    {
        public TextFields(IEnumerable<string> textStrings)
        {
            SetSearchValues(textStrings);
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.TextFields;
    }
}
