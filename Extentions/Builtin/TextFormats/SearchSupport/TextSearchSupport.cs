using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes;
using Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats;

namespace Clipbox.Extentions.BuiltIn.TextFormats.SearchSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{89BCFB06-B1FC-4C0D-BFE6-9643BED5A172}")]
    public class TextSearchSupport : SearchSupportExtension<TextIntermediateFormat>
    {
        protected override string GetSearchTextData(TextIntermediateFormat intermediateFormat)
        {
            return intermediateFormat.Text.ReplaceNonAlphaWith(' ');
        }
    }
}
