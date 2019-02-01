using Clipbox.Win32.Native;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static Clipbox.Win32.Native.NativeEnums;

namespace Clipbox.Win32
{

    [ComImport, Guid("6D67E846-5B9C-4db8-9CBC-DDE12F4254F1"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ITrayDeskband
    {
        [PreserveSig]
        int ShowDeskBand([In, MarshalAs(UnmanagedType.Struct)] ref Guid clsid);
        [PreserveSig]
        int HideDeskBand([In, MarshalAs(UnmanagedType.Struct)] ref Guid clsid);
        [PreserveSig]
        int IsDeskBandShown([In, MarshalAs(UnmanagedType.Struct)] ref Guid clsid);
        [PreserveSig]
        int DeskBandRegistrationChanged();
    }

    public static class DeskbandUtils
    {
        private const string CONFIRMATION_POPUP_CLASS_NAME = "#32770";

        private static Type _trayDeskbandType = System.Type.GetTypeFromCLSID(new Guid("E6442437-6C68-4f52-94DD-2CFED267EFB9"));

        public static bool ShowDeskBand(Guid deskbandGuid, string deskBandName, bool autoConfirm)
        {
            ITrayDeskband obj = null;
            try
            {
                obj = (ITrayDeskband)Activator.CreateInstance(_trayDeskbandType);

                int hr = obj.DeskBandRegistrationChanged();
                if (hr != (int)HRESULT.S_OK)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                hr = obj.IsDeskBandShown(ref deskbandGuid);
                if (hr == (int)HRESULT.S_FALSE)
                {
                    hr = obj.ShowDeskBand(ref deskbandGuid);
                    if (hr != 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                }
                else if (hr != (int)HRESULT.S_OK)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                hr = obj.DeskBandRegistrationChanged();
                if (hr != (int)HRESULT.S_OK)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
            finally
            {
                if (obj != null && Marshal.IsComObject(obj))
                    Marshal.ReleaseComObject(obj);
            }

            if (autoConfirm)
            {
                // attempt to answer with "Yes" to the confirmation popup
                DateTime startTime = DateTime.Now;
                IntPtr hWndButton = IntPtr.Zero;
                do
                {
                    hWndButton = User32.FindWindow(CONFIRMATION_POPUP_CLASS_NAME, deskBandName);

                    if ((DateTime.Now - startTime).TotalSeconds > 10)
                    {
                        return false;
                    }
                }
                while (hWndButton == IntPtr.Zero);

                if (hWndButton == IntPtr.Zero)
                {
                    return false;
                }

                startTime = DateTime.Now;
                while(!User32.IsWindowVisible(hWndButton))
                {
                    if ((DateTime.Now - startTime).TotalSeconds > 10)
                    {
                        return false;
                    }
                }
                
                if (!User32.SetForegroundWindow(hWndButton))
                {
                    throw new Win32Exception();
                }

                if (!User32.PostMessage(hWndButton, (int)KeyboardMessages.WM_KEYDOWN, new IntPtr((int)Keys.Enter), IntPtr.Zero))
                {
                    throw new Win32Exception();
                }


                if (!User32.PostMessage(hWndButton, (int)KeyboardMessages.WM_KEYUP, new IntPtr((int)Keys.Enter), IntPtr.Zero))
                {
                    throw new Win32Exception();
                }
            }

            return true;
        }

        public static void HideDeskband(Guid deskbandGuid)
        {
            ITrayDeskband obj = null;
            try
            {
                obj = (ITrayDeskband)Activator.CreateInstance(_trayDeskbandType);

                int hr = obj.DeskBandRegistrationChanged();
                if (hr != (int)HRESULT.S_OK)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                hr = obj.IsDeskBandShown(ref deskbandGuid);
                if (hr == (int)HRESULT.S_OK)
                {
                    hr = obj.HideDeskBand(ref deskbandGuid);
                    if (hr != 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                }
                else if (hr != (int)HRESULT.S_FALSE)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                hr = obj.DeskBandRegistrationChanged();
                if (hr != (int)HRESULT.S_OK)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
            finally
            {
                if (obj != null && Marshal.IsComObject(obj))
                    Marshal.ReleaseComObject(obj);
            }
        }

        public static bool IsTaskBandShown(Guid deskbandGuid)
        {
            bool isShown = false;
            ITrayDeskband obj = null;
            try
            {
                obj = (ITrayDeskband)Activator.CreateInstance(_trayDeskbandType);

                // refresh the cache
                int hr = obj.DeskBandRegistrationChanged();
                if (hr != (int)HRESULT.S_OK)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                hr = obj.IsDeskBandShown(ref deskbandGuid);
                if (hr == (int)HRESULT.S_OK)
                {
                    isShown = true;
                }
                else if (hr == (int)HRESULT.S_FALSE)
                {
                    isShown = false;
                }
                else
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
            finally
            {
                if (obj != null && Marshal.IsComObject(obj))
                    Marshal.ReleaseComObject(obj);
            }

            return isShown;
        }
    }
}
