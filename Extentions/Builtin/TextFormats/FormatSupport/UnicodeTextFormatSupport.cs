using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System;
using System.Runtime.InteropServices;
using System.Text;
using Clipbox.Win32;
using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Extentions.BuiltIn.TextFormats.FormatSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "Unicode text formats", Version = 0.1)]
    [ClipboardFormat("CF_UNICODETEXT")]
    public class UnicodeTextFormatSupport : ClipboardFormatSupport, IPersistableFormat
    {
        public override IntPtr DataToPtr(ref object data)
        {
            if (!(data is string))
            {
                throw new ArgumentException("data is not a string");
            }

            // thanks to kolibridev's Clippy (in github)
            // Marshal.StringToHGlobalUni allocates using LocalAlloc with LMEM_FIXED
            IntPtr hMemLocal = Marshal.StringToHGlobalUni((string)data);
            try
            {
                int size = ((string)data).Length * 2 + 1;
                return MarshalExt.HMemToHGlobalMemMovable(hMemLocal, (uint)size);
            }
            finally
            {
                Marshal.FreeHGlobal(hMemLocal);
            }
        }

        public override object PtrToData(IntPtr hClipboardData)
        {
            IntPtr hMemLocal = MarshalExt.HGlobalMemMovableToHMem(hClipboardData);
            string str = Marshal.PtrToStringUni(hMemLocal);
            if (str != null)
            {
                // TODO: native strings are usually delimited by a zero byte,
                // but I should check if I need to remove it after PtrToStringUni
                str = str.TrimEnd(new char[] { '\0' });
            }

            return str;
        }

        public object ByteArrayToObject(byte[] byteArray)
        {
            return Encoding.Unicode.GetString(byteArray);
        }

        public byte[] ObjectToByteArray(ref object data)
        {
            string str = data as string;

            if (str != null)
            {
                return Encoding.Unicode.GetBytes(str);
            }

            return null;
        }

        public bool RequiresSameMachine => false;

        public override ClipboardContentClass ContentClass => ClipboardContentClass.Text;

    }
}
