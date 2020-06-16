using Clipbox.Infrastructure.Search;

namespace Clipbox.Services.Interfaces
{
    interface ISearchProviderSelectorService
    {
        ISearchProvider GetSearchProvider();
    }
}
