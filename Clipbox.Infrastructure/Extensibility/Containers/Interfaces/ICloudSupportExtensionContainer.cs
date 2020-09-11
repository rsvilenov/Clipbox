using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.CloudSupportTypes;
using System;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface ICloudSupportExtensionContainer
    {
        IEnumerable<CloudSupportExtension> ResolveAll();

        CloudSupportExtension Resolve(string cloudServiceName);

        IEnumerable<Type> GetRegisteredTypes();
    }
}
