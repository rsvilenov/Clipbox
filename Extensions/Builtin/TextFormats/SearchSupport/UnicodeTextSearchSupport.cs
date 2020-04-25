using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SearchSupport
{
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{CAF7A257-882B-46CE-A26D-679D228EA8B7}")]
    public class UnicodeTextSearchSupport : SearchSupportExtension<UnicodeTextIntermediateFormat>
    {
        protected override string GetSearchTextData(UnicodeTextIntermediateFormat intermediateFormat)
        {
            return intermediateFormat.Text.ReplaceNonAlphaWith(' ');
        }
    }
}
