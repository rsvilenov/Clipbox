using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SearchSupport
{
    [ClipboxExtension(
        description: "Search support for copied Unicode text content",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
    )]
    [IntermediateFormatSupport("{CAF7A257-882B-46CE-A26D-679D228EA8B7}")]
    public class UnicodeTextSearchSupport : SearchSupportExtension<UnicodeTextIntermediateFormat>
    {
        protected override string GetSearchTextData(UnicodeTextIntermediateFormat intermediateFormat)
        {
            return intermediateFormat.Text.ReplaceNonAlphaWith(' ');
        }
    }
}
