using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.SearchTerms
{
    [Serializable]
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

        protected MaxCreationDateSearchTerm(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.CreationDate;
    }
}
