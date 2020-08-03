using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class PersistenceSupportExtensionContainer :
        BaseExtensionContainer<IPersistenceSupport, WeakFactory>,
        IPersistenceSupportExtensionContainer,
        INotifyExtensionResolved
    {
        private ConcurrentDictionary<string, Type> _intermediateFormatGuidExtensionMapping =
            new ConcurrentDictionary<string, Type>();

        public event EventHandler<ExtensionEventArgs> ExtensionResolved;

        public bool CheckIsIntermediateFormatSupported(string intermediateFormatGuid)
        {
            return _intermediateFormatGuidExtensionMapping.ContainsKey(intermediateFormatGuid);
        }

        public IPersistenceSupport ResolvePersistenceSupport(string intermediateFormatGuid)
        {
            if (_intermediateFormatGuidExtensionMapping.ContainsKey(intermediateFormatGuid))
            {
                var extensionInstance = Resolve(_intermediateFormatGuidExtensionMapping[intermediateFormatGuid]);
                ExtensionResolved?.Invoke(this, new ExtensionEventArgs { ExtensionType = extensionInstance.GetType() });
                return extensionInstance;
            }

            return null;
        }

        public override void RegisterExtension(Type type)
        {
            if (!IntermediateFormatSupportAttribute.IsDefinedFor(type))
            {
                throw new InvalidOperationException($"Type {type.FullName} does not have a {nameof(IntermediateFormatSupportAttribute)} attribute");
            }

            string intermediateFormatGuid = IntermediateFormatSupportAttribute.GetIntermediateFormatGuidFor(type);

            if (_intermediateFormatGuidExtensionMapping.ContainsKey(intermediateFormatGuid))
            {
                throw new InvalidOperationException($"There is already an {nameof(IPersistenceSupport)} extension, registered for intermediate format guid {intermediateFormatGuid}");
            }

            _intermediateFormatGuidExtensionMapping.TryAdd(intermediateFormatGuid, type);


            base.RegisterExtension(type);
        }
    }
}
