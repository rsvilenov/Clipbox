using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface ISearchSupportExtensionContainer
    {
        ISearchSupport ResolveSearchSupport(string intermediateFormatGuid);
    }
}
