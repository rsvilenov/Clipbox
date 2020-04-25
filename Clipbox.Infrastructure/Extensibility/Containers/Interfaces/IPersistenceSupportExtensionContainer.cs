using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IPersistenceSupportExtensionContainer
    {
        IPersistenceSupport ResolvePersistenceSupport(string intermediateFormatGuid);

        bool CheckIsIntermediateFormatSupported(string intermediateFormatGuid);
    }
}
