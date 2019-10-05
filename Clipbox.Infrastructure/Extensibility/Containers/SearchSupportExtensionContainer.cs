using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class SearchSupportExtensionContainer :
        BaseExtentionContainer<SearchSupportExtension, WeakFactory>,
        ISearchSupportExtensionContainer,
        INotifyExtentionResolved,
        IExtentionInfoProvider
    {
        private ConcurrentDictionary<string, Type> _clipboardFormatExtentionMapping =
            new ConcurrentDictionary<string, Type>();

        public event EventHandler<ExtentionEventArgs> ExtentionResolved;

        public SearchSupportExtension ResolveSearchSupport(string format)
        {
            if (_clipboardFormatExtentionMapping.ContainsKey(format))
            {
                var extentionInstance = Resolve(_clipboardFormatExtentionMapping[format]);
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
                if (_clipboardFormatExtentionMapping.ContainsKey(format))
                {
                    throw new InvalidOperationException($"There is already an extention, registered for clipboard format {format}");
                }

                _clipboardFormatExtentionMapping.TryAdd(format, type);
            }

            base.RegisterExtention(type);
        }
    }
}
