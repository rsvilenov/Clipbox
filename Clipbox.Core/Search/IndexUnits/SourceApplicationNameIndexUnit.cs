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
            string appName = clipboardItem.Source.SourceWindowInfo.ApplicationInfo.ProductName;

            string appFileName = string.Empty;
            string appPath = clipboardItem.Source.SourceWindowInfo.ApplicationInfo.ApplicationPath;
            if (string.IsNullOrWhiteSpace(appPath)
                && appPath.All(c => !Path.GetInvalidPathChars().Contains(c)))
            {
                appName = Path.GetFileNameWithoutExtension(appPath);
            }

            return new string[]
            {
                appName,
                appFileName
            };
        }
    }
}
