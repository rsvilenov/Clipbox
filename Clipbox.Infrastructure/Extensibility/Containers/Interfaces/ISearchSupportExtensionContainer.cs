using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface ISearchSupportExtensionContainer
    {
        ISearchSupport ResolveSearchSupport(string intermediateFormatGuid);
    }
}
