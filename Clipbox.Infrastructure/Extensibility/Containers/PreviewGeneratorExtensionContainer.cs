using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class PreviewGeneratorExtensionContainer :
        BaseExtensionContainer<IPreviewGenerator, RegularFactory>,
        IPreviewGeneratorExtensionContainer,
        INotifyExtensionResolved
    {
        private ConcurrentDictionary<string, TypeAndPriority> _intermediateFormatGuidExtensionMapping =
            new ConcurrentDictionary<string, TypeAndPriority>();

        public event EventHandler<ExtensionEventArgs> ExtensionResolved;

        public IPreviewGenerator ResolveClipboardPreviewGenerator(string formatGuid)
        {
            if (_intermediateFormatGuidExtensionMapping.ContainsKey(formatGuid))
            {
                var extensionInstance = Resolve(_intermediateFormatGuidExtensionMapping[formatGuid].Type);
                ExtensionResolved?.Invoke(this, new ExtensionEventArgs { ExtensionType = extensionInstance.GetType() });
                return extensionInstance;
            }

            return null;
        }

        public KeyValuePair<string, IPreviewGenerator>? ResolveClipboardPreviewGeneratorWithHighestPriority(string[] formatGuids)
        {
            Type extensionType = null;
            int highestPriority = int.MinValue;
            string finalFormatGuid = null;
            foreach (string formatGuid in formatGuids)
            {
                if (_intermediateFormatGuidExtensionMapping.ContainsKey(formatGuid))
                {
                    TypeAndPriority tp = _intermediateFormatGuidExtensionMapping[formatGuid];
                    if (tp.Priority > highestPriority)
                    {
                        highestPriority = tp.Priority;
                        extensionType = tp.Type;
                        finalFormatGuid = formatGuid;
                    }
                }
            }

            if (extensionType != null)
            {
                var extensionInstance = Resolve(extensionType);
                ExtensionResolved?.Invoke(this, new ExtensionEventArgs { ExtensionType = extensionType });
                return new KeyValuePair<string, IPreviewGenerator>(finalFormatGuid, extensionInstance);
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
                throw new InvalidOperationException($"There is already an {nameof(ISearchSupport)} extension, registered for intermediate format guid {intermediateFormatGuid}");
            }

            int previewPriority = 0;
            if (PreviewPriorityAttribute.IsDefinedFor(type))
            {
                previewPriority = PreviewPriorityAttribute.GetPreviewPriorityLevelFor(type);
            }

            _intermediateFormatGuidExtensionMapping.TryAdd(intermediateFormatGuid, new TypeAndPriority(type, previewPriority));

            base.RegisterExtension(type);
        }

        public IEnumerable<Type> GetRegisteredTypes()
        {
            return _intermediateFormatGuidExtensionMapping
                .Values
                .Select(v => v.Type);
        }

        private class TypeAndPriority
        {
            public TypeAndPriority(Type type, int priority)
            {
                Type = type;
                Priority = priority;
            }

            public Type Type { get; }

            public int Priority { get; }
        }
    }
}
