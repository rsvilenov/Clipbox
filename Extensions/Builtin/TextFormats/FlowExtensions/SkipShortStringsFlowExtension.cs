using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions
{
    [ClipboxExtension]
    public class SkipShortStringsFlowExtension : FlowExtension
    {
        public override void OnItemStoring(IWindowInfo sourceWindowInfo, IEnumerable<IIntermediateFormat> originalIntermediateFormats, ref IList<IIntermediateFormat> modifiedIntermediateFormats, ref bool cancelStoreToItem)
        {
            int? textLength = originalIntermediateFormats
                .Where(ifo => ifo is TextIntermediateFormat)
                .Select(ifo => ((TextIntermediateFormat)ifo).Text?.Length)
                .Union(
                    originalIntermediateFormats
                    .Where(ifo => ifo is UnicodeTextIntermediateFormat)
                    .Select(ifo => ((UnicodeTextIntermediateFormat)ifo).Text?.Length))
                .Union(originalIntermediateFormats
                    .Where(ifo => ifo is UnicodeTextIntermediateFormat)
                    .Select(ifo => ((UnicodeTextIntermediateFormat)ifo).Text?.Length))
                .FirstOrDefault(len => len != null);

            int minTextLength = GetSetting<int>(ExtensionSettingNames.MinTextLengthToStore);

            if (minTextLength > 0 /* 0 means infinite */
                && textLength.HasValue 
                && textLength.Value < minTextLength)
            {
                cancelStoreToItem = true;
            }
        }
    }
}
