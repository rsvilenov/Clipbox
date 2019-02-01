using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Extentions.BuiltIn.HdropFormat.Enums;
using System;
using System.Linq;

namespace Clipbox.Extentions.BuiltIn.HdropFormat
{
    internal static class Common
    {
        internal static DropEffects? GetDropEffectFromClipboardItem(IClipboardItem clipboardItem)
        {
            DropEffects? dropEffect = null;

            var dropEffectDataObject = clipboardItem.ClipboardDataObjects.FirstOrDefault(cdo => cdo.Format == "Preferred DropEffect");
            if (dropEffectDataObject != null &&
                dropEffectDataObject.OriginalData != null &&
                // although the DropEffects enum is a Flags enum, the values of the prefferred drop effect are exact members
                Enum.IsDefined(typeof(DropEffects), dropEffectDataObject.OriginalData))
            {
                dropEffect = (DropEffects)dropEffectDataObject.OriginalData;
            }

            return dropEffect;
        }
    }
}
