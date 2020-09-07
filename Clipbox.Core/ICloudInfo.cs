using System;

namespace Clipbox.Core
{
    public interface ICloudInfo
    {
        Type ExtensionObjectType { get; set; }

        string CloudServiceName { get; }

        byte[] IconImageData { get; }
    }
}
