using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search
{
    public interface ISearchTerm
    {
        SearchCategory Category { get; }
        
        IEnumerable<string> SearchStringValues { get; }

        bool Validate();
    }
}
