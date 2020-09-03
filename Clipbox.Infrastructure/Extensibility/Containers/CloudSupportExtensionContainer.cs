using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.CloudSupportTypes;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class CloudSupportExtensionContainer :
        BaseExtensionContainer<CloudSupportExtension, WeakFactory>,
        ICloudSupportExtensionContainer
    {
        public IEnumerable<CloudSupportExtension> ResolveAll()
        {
            foreach (Type t in base.RegisteredTypes)
            {
                yield return base.Resolve(t);
            }
        }
    }
}
