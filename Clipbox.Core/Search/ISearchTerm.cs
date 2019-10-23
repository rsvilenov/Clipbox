using Clipbox.Core.Search.Enums;

namespace Clipbox.Core.Search
{
    public interface ISearchTerm : ISearchObject
    {   
        TermOccurrence Occurrence { get; }
    }
}
