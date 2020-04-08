using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clipbox.Core.Search.IndexUnits
{
    public class SourceApplicationNameIndexUnit : IndexUnitBase<string>
    {
        public SourceApplicationNameIndexUnit(IClipboardItem clipboardItem)
            : base(clipboardItem) { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceApplicationName;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            string appFileName = string.Empty;
            string appPath = clipboardItem.Source.SourceWindowInfo.ApplicationInfo.ApplicationPath;
            if (!string.IsNullOrWhiteSpace(appPath)
                && appPath.All(c => !Path.GetInvalidPathChars().Contains(c)))
            {
                appFileName = Path.GetFileName(appPath);
            }

            return new string[]
            {
                clipboardItem.Source.SourceWindowInfo.ApplicationInfo.ProductName,
                clipboardItem.Source.SourceWindowInfo.ApplicationInfo.FileDescription,
                appPath,
                appFileName
            }.Where(s => !string.IsNullOrWhiteSpace(s));
        }
    }
}
