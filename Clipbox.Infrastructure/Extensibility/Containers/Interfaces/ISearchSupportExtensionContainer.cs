using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface ISearchSupportExtensionContainer
    {
        SearchSupportExtension ResolveSearchSupport(string format);
    }
}
