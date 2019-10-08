using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.SearchSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_HDROP")]
    public class HdropSearchSupport : SearchSupportExtension
    {
        public override string GetSearchTextData(object data)
        {
            if (!(data is string[] hdropPaths))
            {
                throw new ArgumentException("clipboardData is not a string[]");
            }

            StringBuilder fileNames = new StringBuilder();
            
            foreach (string path in hdropPaths)
            {
                // sometimes I get control characters instead of paths in the beginning of the hdrop string
                if (path.Any(cc => char.IsControl(cc)))
                {
                    continue;
                }

                try
                {
                    fileNames.AppendFormat(" {0} ", Path.GetFileNameWithoutExtension(path));
                }
                catch (ArgumentException)
                { }
            }

            return fileNames.ToString();
        }
    }
}
