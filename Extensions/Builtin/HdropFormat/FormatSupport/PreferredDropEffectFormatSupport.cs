using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Runtime.InteropServices;
using Clipbox.Extensions.BuiltIn.HdropFormat.IntermediateFormats;
using Clipbox.Extensions.BuiltIn.HdropFormat.Enums;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.FormatSupport
{
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "Paste explorer action: Cut, copy or copy shortcut", Version = 0.1)]
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
