using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SearchSupport
{
    [ClipboxExtension(
    description: "Search support for copied ANSI text content",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    [IntermediateFormatSupport("{89BCFB06-B1FC-4C0D-BFE6-9643BED5A172}")]
    public class TextSearchSupport : SearchSupportExtension<TextIntermediateFormat>
    {
        protected override string GetSearchTextData(TextIntermediateFormat intermediateFormat)
        {
            return intermediateFormat.Text.ReplaceNonAlphaWith(' ');
        }
    }
}
