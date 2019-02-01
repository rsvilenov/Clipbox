using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IExtentionInfoProvider
    {
        IExtentionInfo GetExtentionInfo(IClipboxExtention extention);
    }
}
