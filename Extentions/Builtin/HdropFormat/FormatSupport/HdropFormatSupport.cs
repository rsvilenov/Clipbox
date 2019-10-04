using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.FormatSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "File collection from explorer", Version = 0.1)]
    [ClipboardFormat("CF_HDROP")]
    public class HdropFormatSupport : ClipboardFormatSupport, IPersistableFormat
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DROPFILES
        {
            public int pFiles;
            public Point pt;
            public bool fNC;
            public bool fWide;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, int cch);

        public bool RequiresSameMachine
        {
            get
            {
                return true; // the paths will hardly be present on another machine
            }
        }

        public object ByteArrayToObject(byte[] byteArray)
        {
            if (byteArray == null)
            {
                throw new ArgumentNullException("bytaArray");
            }

            string hdropString = Encoding.Unicode.GetString(byteArray);

            string[] filePaths = hdropString.Split(new string[] { "\0" }, StringSplitOptions.RemoveEmptyEntries);
            return filePaths;
        }


        public byte[] ObjectToByteArray(ref object data)
        {
            string[] stringArray = data as string[];
            if (stringArray == null)
            {
                throw new ArgumentException("data must be string[]");
            }
            // convert the string array to hdrop string and then to bytearray
            string hdropString = StringArrayToNullDelimitedString(stringArray);
            return Encoding.Unicode.GetBytes(hdropString);
        }

        private string StringArrayToNullDelimitedString(string[] stringArray)
        {
            string zeroDelimitedString = stringArray.
                Select(str => str + char.MinValue).
                Aggregate((str1, str2) => str1 + str2);

            return zeroDelimitedString + char.MinValue;
        }

        public override IntPtr DataToPtr(ref object data)
        {
            if (!(data is string[]))
            {
                throw new ArgumentException("data is not a string[]");
            }

            string pathsString = StringArrayToNullDelimitedString((string[])data);
            var pathsBytes = Encoding.Unicode.GetBytes(pathsString);
            
            DROPFILES structure = new DROPFILES();
            structure.pFiles = Marshal.SizeOf(structure);
            structure.fWide = true;


            int totalSize = Marshal.SizeOf(structure) + pathsBytes.Length;


            IntPtr hGlobal = MarshalExt.CreateHGlobalMovable(totalSize, (hGlobalLocked) =>
            {
                Marshal.StructureToPtr(structure, hGlobalLocked, false);
                IntPtr pathsPtr = new IntPtr(hGlobalLocked.ToInt64() + Marshal.SizeOf(structure));

                Marshal.Copy(pathsBytes, 0, pathsPtr, pathsBytes.Length);
            });

            return hGlobal;
        }

        public override object PtrToData(IntPtr hClipboardData)
        {
            IntPtr hDrop = Kernel32.GlobalLock(hClipboardData);

            List<string> pathList = new List<string>();

            try
            {
                // https://msdn.microsoft.com/en-us/library/windows/desktop/bb776408(v=vs.85).aspx

                // get the file number
                int fileNum = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);

                if (fileNum == 0)
                {
                    // the docs say that 0 means error, but fail to specify whether we can get the 
                    // error by calling GetLastError, so we will just assume that
                    MarshalExt.ThrowOnWin32Error();
                }
                
                if (fileNum > 0)
                {
                    for (uint i = 0; i < fileNum; i++)
                    {
                        // get the length of the filename (this is the number of characters only - the null terminator is not included)
                        int pathLength = DragQueryFile(hDrop, i, null, 0);
                        if (pathLength == 0)
                        {
                            MarshalExt.ThrowOnWin32Error();
                        }

                        // the filename is a null terminated string, so we need to increase the length of the buffer by 1.
                        // DragQueryFile works with number of characters, not with bytes, so the encoding doesn't matter here
                        pathLength += 1;

                        // allocate a buffer
                        StringBuilder lpszFile = new StringBuilder(pathLength);

                        // get the actual filename
                        int success = DragQueryFile(hDrop, i, lpszFile, lpszFile.Capacity);
                        if (success == 0)
                        {
                            MarshalExt.ThrowOnWin32Error();
                        }

                        if (lpszFile.Length > 0)
                        {
                            pathList.Add(lpszFile.ToString());
                        }
                    }
                }
            }
            finally
            {
                Kernel32.GlobalUnlock(hDrop);
            }
            
            return pathList.ToArray();
        }

        public override ClipboardContentClass ContentClass => ClipboardContentClass.File;

    }
}
