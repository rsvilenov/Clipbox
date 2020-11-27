using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.HdropFormat.Enums;
using Clipbox.Extensions.BuiltIn.HdropFormat.IntermediateFormats;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Runtime.InteropServices;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.FormatSupport
{
    [ClipboxExtension(
        description: "Storing the paste action when copying files from Windows Explorer. (The preferred drop effect, which can be either Cut, Copy or Copy shortcut)",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
        )]
    [ClipboardFormat("Preferred DropEffect")]
    public class PreferredDropEffectFormatSupport : ClipboardFormatSupport<PreferredDropEffectIntermediateFormat>
    {
        protected override PreferredDropEffectIntermediateFormat GetIntermediateFormat(IntPtr hClipboardData)
        {
            IntPtr hEffect = Kernel32.GlobalLock(hClipboardData);
            try
            {
                DropEffects dropEffect = DropEffects.DROPEFFECT_COPY;

                UInt32 drEffect = Marshal.PtrToStructure<UInt32>(hEffect);

                if (Enum.IsDefined(typeof(DropEffects), drEffect))
                {
                    dropEffect = (DropEffects)drEffect;
                }

                return new PreferredDropEffectIntermediateFormat(dropEffect);
            }
            finally
            {
                Kernel32.GlobalUnlock(hClipboardData);
            }
        }

        protected override IntPtr GetPtr(PreferredDropEffectIntermediateFormat intermediateFormat)
        {
            // from MSDN: Drag sources and drop targets should carefully mask these values appropriately before 
            // comparing. They should never compare a DROPEFFECT against, say, DROPEFFECT_COPY
            
            UInt32 dropEffect = (UInt32)intermediateFormat.DropEffect;

            IntPtr hGlobal = MarshalExt.CreateHGlobalMovable(Marshal.SizeOf(typeof(UInt32)),
                (hGlobalMovable) =>
                {
                    Marshal.StructureToPtr<UInt32>(dropEffect, hGlobalMovable, false);
                });

            return hGlobal;
        }
    }
}
