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
        private ConcurrentDictionary<string, Type> _clipboardFormatPreviewExtentionMapping =
            new ConcurrentDictionary<string, Type>();

        public event EventHandler<ExtentionEventArgs> ExtentionResolved;

        public IPreviewGenerator ResolveClipboardPreviewGenerator(string format)
        {
            if (_clipboardFormatPreviewExtentionMapping.ContainsKey(format))
            {
                var extentionInstance = Resolve(_clipboardFormatPreviewExtentionMapping[format]);
                ExtentionResolved?.Invoke(this, new ExtentionEventArgs { ExtentionType = extentionInstance.GetType() });
                return extentionInstance;
            }

            return null;
        }

        public override void RegisterExtention(Type type)
        {
            if (!ClipboardFormatAttribute.IsDefinedFor(type))
            {
                throw new InvalidOperationException($"Type {type.FullName} does not have a ClipboardFormat attribute");
            }

            foreach (var format in ClipboardFormatAttribute.GetFormats(type))
            {
                if (_clipboardFormatPreviewExtentionMapping.ContainsKey(format))
                {
                    throw new InvalidOperationException($"There is already apreview handler, registered for clipboard format {format}");
                }

                _clipboardFormatPreviewExtentionMapping.TryAdd(format, type);
            }

            base.RegisterExtention(type);
        }
    }
}
