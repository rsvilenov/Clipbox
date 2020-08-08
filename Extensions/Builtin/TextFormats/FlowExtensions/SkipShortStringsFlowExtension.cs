using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions.Common;
using System.Collections.Generic;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions
{
    [ClipboxExtension(
    description: "Skips storing words shorted than N characters.",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    public class SkipShortStringsFlowExtension : FlowExtension
    {
        /// <summary>
        /// Take the highest priority.
        /// </summary>
        public override int FlowOrder => -100;

        public override void OnItemStoring(IWindowInfo sourceWindowInfo, IEnumerable<IIntermediateFormat> originalIntermediateFormats, ref IList<IIntermediateFormat> modifiedIntermediateFormats, ref bool cancelStoreToItem)
        {
            string text = GetTextFromIntermediateFormat.GetText(originalIntermediateFormats);
            
            if (text != null)
            {
                int minTextLength = GetSetting<int>(ExtensionSettingNames.MinTextLengthToStore);

                if (minTextLength > 0 /* 0 means infinite */
                    && text.Length < minTextLength)
                {
                    cancelStoreToItem = true;
                }
            }
        }
    }
}
