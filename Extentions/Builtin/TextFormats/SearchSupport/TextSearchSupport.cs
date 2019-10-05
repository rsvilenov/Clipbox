using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes;
using System;

namespace Clipbox.Extentions.BuiltIn.TextFormats.SearchSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("Text")]
    [ClipboardFormat("CF_UNICODETEXT")]
    public class TextSearchSupport : SearchSupportExtension
    {
        public override string GetSearchTextData(object data)
        {
            if (!(data is string text))
            {
                throw new ArgumentException("data is null or not a string");
            }

            return text;
        }
    }
}
