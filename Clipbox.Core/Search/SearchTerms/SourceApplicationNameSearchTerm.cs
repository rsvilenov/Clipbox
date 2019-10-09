using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;
using System.IO;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceApplicationNameSearchTerm : SearchTermBase<string>
    {
        public SourceApplicationNameSearchTerm(IEnumerable<string> sourceApplicationNames)
            : base(sourceApplicationNames)
        { }

        public SourceApplicationNameSearchTerm(IClipboardItem clipboardItem)
            : base(clipboardItem)
        { }

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            string appName = string.Empty;
            string appPath = clipboardItem.Source.SourceWindowInfo.ApplicationPath;
            if (string.IsNullOrWhiteSpace(appPath))
            { 
                appName = Path.GetFileNameWithoutExtension(appPath);
            }

            return new string[]
            {
                appName
            };
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.SourceApplicationName;

    }
}
