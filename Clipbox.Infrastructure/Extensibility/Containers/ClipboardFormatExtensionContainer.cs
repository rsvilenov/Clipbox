using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class ClipboardFormatExtensionContainer : 
        BaseExtensionContainer<IClipboardFormatSupport, RegularFactory>, 
        IClipboardFormatExtensionContainer, 
        INotifyExtensionResolved
    {
        private ConcurrentDictionary<string, Type> _clipboardFormatExtensionMapping = 
            new ConcurrentDictionary<string, Type>();

        public event EventHandler<ExtensionEventArgs> ExtensionResolved;

        public IClipboardFormatSupport ResolveClipboardFormatSupport(string format)
        {
            if (_clipboardFormatExtensionMapping.ContainsKey(format))
            {
                var extensionInstance = Resolve(_clipboardFormatExtensionMapping[format]);
                ExtensionResolved?.Invoke(this, new ExtensionEventArgs { ExtensionType = extensionInstance.GetType() });
                return extensionInstance;
            }

            return null;
        }

        public override void RegisterExtension(Type type)
        {
            if (!ClipboardFormatAttribute.IsDefinedFor(type))
            {
                throw new InvalidOperationException($"Type {type.FullName} does not have a ClipboardFormat attribute");
            }

            foreach (var format in ClipboardFormatAttribute.GetFormats(type))
            {
                if (_clipboardFormatExtensionMapping.ContainsKey(format))
                {
                    throw new InvalidOperationException($"There is already an extension, registered for clipboard format {format}");
                }

                _clipboardFormatExtensionMapping.TryAdd(format, type);
            }

            base.RegisterExtension(type);
        }
    }
}
