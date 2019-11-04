using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class ClipboardFormatPreviewExtentionContainer :
        BaseExtentionContainer<IPreviewGenerator, RegularFactory>,
        IClipboardFormatPreviewExtentionContainer,
        INotifyExtentionResolved
    {
        private ConcurrentDictionary<string, Type> _intermediateFormatGuidExtentionMapping =
            new ConcurrentDictionary<string, Type>();

        public event EventHandler<ExtentionEventArgs> ExtentionResolved;

        public IPreviewGenerator ResolveClipboardPreviewGenerator(string formatGuid)
        {
            if (_intermediateFormatGuidExtentionMapping.ContainsKey(formatGuid))
            {
                var extentionInstance = Resolve(_intermediateFormatGuidExtentionMapping[formatGuid]);
                ExtentionResolved?.Invoke(this, new ExtentionEventArgs { ExtentionType = extentionInstance.GetType() });
                return extentionInstance;
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

            _intermediateFormatGuidExtentionMapping.TryAdd(intermediateFormatGuid, type);

            base.RegisterExtention(type);
        }
    }
}
