using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Extensions.BuiltIn.HdropFormat.IntermediateFormats;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.FlowExtensions
{
    [ClipboxExtension]
    public class RemoveDeadFilePaths : FlowExtension
    {
        public override IHouseKeepingTask RegisterHousekeepingTask()
        {
            return new HouseKeepingTask
            {
                ClipboardFormat = "CF_HDROP",
                RequiresRestoredState = true,
                Regularity = HouseKeepingRegularity.UponViewClipboardContents
            };
        }

        public override IHouseKeepingResult OnHousekeepingPerformed(IClipboardItem item, string clipboradFormat)
        {
            List<int> cdoIndicesToRemove = new List<int>();
            bool shouldRemoveItem = false;

            if (item.ClipboardDataObjects.Any(cdo => cdo.ClipboardFormatName == "CF_HDROP"))
            {
                int? dibIndex = null;

                // When we copy an image in Firefox, that image is saved in a temp file. Fireox 
                // places in the clipboard both a DIB and the path to the temp file.
                // As a result we can conveniently paste the copied web image not only as an image,
                // but directly as a file as well, but that complicates my task in this extension...
                var dibCdo = item.ClipboardDataObjects.FirstOrDefault(cdo => cdo.ClipboardFormatName == "CF_DIBV5");
                if (dibCdo != null)
                {
                    dibIndex = item.ClipboardDataObjects.Select((v, i) => new { cdo = v, index = i }).
                        Where(obj => obj.cdo.ClipboardFormatName == "CF_DIBV5").Select(obj => obj.index).FirstOrDefault();
                }
                
                var hdropTarget = item.ClipboardDataObjects.First(cdo => cdo.ClipboardFormatName == "CF_HDROP");
                if (hdropTarget.IntermediateFormatObject is FilesystemPathsIntermediateFormat)
                {
                    IEnumerable<string> allPaths = ((FilesystemPathsIntermediateFormat)hdropTarget.IntermediateFormatObject).Paths;
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
