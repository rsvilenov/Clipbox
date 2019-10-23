using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search
{
    public interface ISearchObject
    {
        SearchCategory Category { get; }

        IEnumerable<string> StringValues { get; }

        bool Validate();
    }
}
