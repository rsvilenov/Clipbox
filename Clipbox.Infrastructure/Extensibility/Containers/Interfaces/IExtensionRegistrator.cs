using System;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IExtensionRegistrator
    {
        void RegisterExtension(Type type);
    }
}
