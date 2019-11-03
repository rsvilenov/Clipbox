using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IPersistenceSupportExtensionContainer
    {
        IPersistenceSupport ResolvePersistenceSupport(string intermediateFormatGuid);
    }
}
