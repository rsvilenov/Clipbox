using Clipbox.Infrastructure.Threading;
using Clipbox.Win32.Native;
using System;
using System.ComponentModel;

using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Clipbox.ExtendedClipboard.OleInftastructure
{
    public class OleDataTransferService
    {
        internal void Initialize()
        {
            ThreadValidator.ValidateSTA();
            // Initialize Ole services.
            // Balanced with OleUninitialize call in OnDispatcherShutdown.
            int hr = Ole32.OleInitialize(IntPtr.Zero);

            if (hr < 0)
            {
                throw new Win32Exception(hr, "OleInitialize");
            }
        }

        internal void Uninitialize()
        {
            Ole32.OleUninitialize();
        }

        /// <summary>
        /// Flush the clipboard. This does not clear the clipboard, but rather acts a bit like stream.Flush()...
        /// </summary>
        internal void OleFlushClipboard()
        {
            ThreadValidator.ValidateSTA();

            /// From msdn: https://msdn.microsoft.com/en-us/library/windows/desktop/ms679707(v=vs.85).aspx
            /// OleFlushClipboard renders the data from a data object onto the clipboard and releases the IDataObject pointer to the data object. 
            /// While the application that put the data object on the clipboard is running, the clipboard holds only a pointer to the data object, thus saving memory. 
            /// If you are writing an application that acts as the source of a clipboard operation, you can call the OleFlushClipboard function when your application is closed, 
            /// such as when the user exits from your application. Calling OleFlushClipboard enables pasting and paste-linking of OLE objects after application shutdown.
            int hr = Ole32.OleFlushClipboard();
            if (hr < 0)
            {
                throw new Win32Exception(hr, "OleFlushClipboard");
            }
        }


        internal int OleSetClipboard(IComDataObject dataObject)
        {
            ThreadValidator.ValidateSTA();

            return Ole32.OleSetClipboard(dataObject);
        }

        internal int OleGetClipboard(ref IComDataObject dataObject)
        {
            ThreadValidator.ValidateSTA();

            return Ole32.OleGetClipboard(ref dataObject);
        }
    }
}
