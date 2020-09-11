using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.CloudSupportTypes;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class CloudSupportExtensionContainer :
        BaseExtensionContainer<CloudSupportExtension, WeakFactory>,
        ICloudSupportExtensionContainer
    {
        private ConcurrentDictionary<string, Type> _cloudServiceNameExtensionMapping =
            new ConcurrentDictionary<string, Type>();

        public IEnumerable<CloudSupportExtension> ResolveAll()
        {
            foreach (Type t in base.RegisteredTypes)
            {
                yield return base.Resolve(t);
            }
        }

        public IEnumerable<Type> GetRegisteredTypes()
        {
            return base.RegisteredTypes;
        }

        public CloudSupportExtension Resolve(string cloudServiceName)
        {
            if (_cloudServiceNameExtensionMapping.ContainsKey(cloudServiceName))
            {
                return Resolve(_cloudServiceNameExtensionMapping[cloudServiceName]);
            }

            return null;
        }

        public override void RegisterExtension(Type type)
        {
            if (!CloudSupportAttribute.IsDefinedFor(type))
            {
                throw new InvalidOperationException($"Type {type.FullName} does not have a CloudSupport attribute");
            }

            string name = CloudSupportAttribute.GetCloudInfo(type).CloudServiceName;
            if (_cloudServiceNameExtensionMapping.ContainsKey(name))
            {
                throw new InvalidOperationException($"There is already an extension, registered for this cloud service name {name}");
            }

            _cloudServiceNameExtensionMapping.TryAdd(name, type);

            base.RegisterExtension(type);
        }
    }
}
