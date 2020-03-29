using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Search.SearchTerms
{
    public class MaxCreationDateSearchTerm : SearchTermBase<long>
    {
        public MaxCreationDateSearchTerm(IEnumerable<long> dateUtcTicks)
            : base(dateUtcTicks)
        {
            if (dateUtcTicks.Count() > 1)
            {
                throw new ArgumentOutOfRangeException($"{dateUtcTicks} should contain only one item.");
            }
        }

        public MaxCreationDateSearchTerm()
            : base()
        { }

        protected override SearchCategory SearchCategory => SearchCategory.CreationDate;
    }
}
