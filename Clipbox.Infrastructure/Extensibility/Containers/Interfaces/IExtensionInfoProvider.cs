using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IExtensionInfoProvider
    {
        IExtensionInfo GetExtensionInfo(IClipboxExtension extension);
    }
}
