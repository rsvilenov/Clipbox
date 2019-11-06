using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class PreviewGeneratorExtentionContainer :
        BaseExtentionContainer<IPreviewGenerator, RegularFactory>,
        IPreviewGeneratorExtentionContainer,
        INotifyExtentionResolved
    {
        private ConcurrentDictionary<string, TypeAndPriority> _intermediateFormatGuidExtentionMapping =
            new ConcurrentDictionary<string, TypeAndPriority>();

        public event EventHandler<ExtentionEventArgs> ExtentionResolved;

        public IPreviewGenerator ResolveClipboardPreviewGenerator(string formatGuid)
        {
            if (_intermediateFormatGuidExtentionMapping.ContainsKey(formatGuid))
            {
                var extentionInstance = Resolve(_intermediateFormatGuidExtentionMapping[formatGuid].Type);
                ExtentionResolved?.Invoke(this, new ExtentionEventArgs { ExtentionType = extentionInstance.GetType() });
                return extentionInstance;
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
                if (_intermediateFormatGuidExtentionMapping.ContainsKey(formatGuid))
                {
                    TypeAndPriority tp = _intermediateFormatGuidExtentionMapping[formatGuid];
                    if (tp.Priority > highestPriority)
                    {
                        extensionType = tp.Type;
                        finalFormatGuid = formatGuid;
                    }
                }
            }

            if (extensionType != null)
            {
                var extentionInstance = Resolve(extensionType);
                ExtentionResolved?.Invoke(this, new ExtentionEventArgs { ExtentionType = extensionType });
                return new KeyValuePair<string, IPreviewGenerator>(finalFormatGuid, extentionInstance);
            }

            return null;
        }



        public override void RegisterExtention(Type type)
        {
            if (!IntermediateFormatSupportAttribute.IsDefinedFor(type))
            {
                throw new InvalidOperationException($"Type {type.FullName} does not have a {nameof(IntermediateFormatSupportAttribute)} attribute");
            }

            string intermediateFormatGuid = IntermediateFormatSupportAttribute.GetIntermediateFormatGuidFor(type);

            if (_intermediateFormatGuidExtentionMapping.ContainsKey(intermediateFormatGuid))
            {
                throw new InvalidOperationException($"There is already an {nameof(ISearchSupport)} extention, registered for intermediate format guid {intermediateFormatGuid}");
            }

            int previewPriority = 0;
            if (PreviewPriorityAttribute.IsDefinedFor(type))
            {
                previewPriority = PreviewPriorityAttribute.GetPreviewPriorityLevelFor(type);
            }

            _intermediateFormatGuidExtentionMapping.TryAdd(intermediateFormatGuid, new TypeAndPriority(type, previewPriority));

            base.RegisterExtention(type);
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
