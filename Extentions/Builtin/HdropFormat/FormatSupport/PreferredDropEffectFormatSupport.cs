using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Enums;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Runtime.InteropServices;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.FormatSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "Paste explorer action: Cut, copy or copy shortcut", Version = 0.1)]
    [ClipboardFormat("Preferred DropEffect")]
    public class PreferredDropEffectFormatSupport : ClipboardFormatSupport
    {
        public override IntPtr DataToPtr(ref object data)
        {
            // from MSDN: Drag sources and drop targets should carefully mask these values appropriately before 
            // comparing. They should never compare a DROPEFFECT against, say, DROPEFFECT_COPY
            if (!(data is UInt32))
            {
                throw new ArgumentException("data should be UInt32");
            }

            UInt32 dropEffect = (UInt32)data;

            IntPtr hGlobal = MarshalExt.CreateHGlobalMovable(Marshal.SizeOf(typeof(UInt32)), 
                (hGlobalMovable) =>
                {
                    Marshal.StructureToPtr<UInt32>(dropEffect, hGlobalMovable, false);
                });

            return hGlobal;
        }

        public override object PtrToData(IntPtr hClipboardData)
        {

            IntPtr hEffect = Kernel32.GlobalLock(hClipboardData);

            try
            {
                return Marshal.PtrToStructure<UInt32>(hEffect);
            }
            finally
            {
                Kernel32.GlobalUnlock(hClipboardData);
            }
        }

        public override ClipboardContentClass ContentClass => ClipboardContentClass.AuxilliaryData;
    }
}
