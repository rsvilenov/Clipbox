using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.FlowExtentions
{
    [ClipboxExtention]
    public class RemoveDeadFilePaths : FlowExtention
    {
        public override IHouseKeepingResult OnHousekeepingPerformed(IClipboardItem item)
        {
            List<int> cdoIndicesToRemove = new List<int>();
            bool shouldRemoveItem = false;

            if (item.ClipboardDataObjects.Any(cdo => cdo.Format == "CF_HDROP"))
            {
                
                int? dibIndex = item.ClipboardDataObjects.Select((v, i) => new { cdo = v, index = i }).
                    Where(obj => obj.cdo.Format == "CF_DIBV5").Select(obj => obj.index).FirstOrDefault();

                var hdropTarget = item.ClipboardDataObjects.First(cdo => cdo.Format == "CF_HDROP");
                if (hdropTarget.OriginalData is string[])
                {
                    string[] allPaths = (string[])hdropTarget.OriginalData;
                    IEnumerable<string> nonExistentPaths = allPaths.Where(path => !FileOrDirectoryExists(path));
                    // if any of the files in the item do not exist any more, 
                    // remove the whole item (for now)
                    if (allPaths.Any(path => nonExistentPaths.Contains(path)))
                    {
                        if (dibIndex.HasValue)
                        {
                            int[] indicesToRemove = item.ClipboardDataObjects.
                                Select((v, i) => new { cdo = v, index = i }).
                                Where(obj => obj.index != dibIndex).
                                Select(obj => obj.index).
                                ToArray();

                            cdoIndicesToRemove.AddRange(indicesToRemove);
                        }
                        else
                        {
                            shouldRemoveItem = true;
                        }
                    }
                }
            }

            return new HouseKeepingResult
            {
                ShouldRemoveItem = shouldRemoveItem,
                ClipboardDataObjectsToRemove = cdoIndicesToRemove
            };
        }

        private bool FileOrDirectoryExists(string name)
        {
            return (Directory.Exists(name) || File.Exists(name));
        }
    }
}
