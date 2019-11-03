using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class SearchSupportExtensionContainer :
        BaseExtentionContainer<ISearchSupport, WeakFactory>,
        ISearchSupportExtensionContainer,
        INotifyExtentionResolved,
        IExtentionInfoProvider
    {
        private ConcurrentDictionary<string, Type> _intermediateFormatGuidExtentionMapping =
            new ConcurrentDictionary<string, Type>();

        public event EventHandler<ExtentionEventArgs> ExtentionResolved;

        public ISearchSupport ResolveSearchSupport(string intermediateFormatGuid)
        {
            if (_intermediateFormatGuidExtentionMapping.ContainsKey(intermediateFormatGuid))
            {
                var extentionInstance = Resolve(_intermediateFormatGuidExtentionMapping[intermediateFormatGuid]);
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
