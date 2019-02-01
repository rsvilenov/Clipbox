using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32;
using System.Globalization;

namespace Clipbox.Win32
{
    /// <summary>
    /// WARNING: This code uses an undocumented COM service. We are not suppose to change tray settings from our program. Users have the choice to do that from the Windows Settings app.
    /// The COM definitions and know-how in this code come mostly from Squirrel
    /// https://github.com/mixmaxhq/electron-promote-windows-tray-items/blob/3d3128100a44a075386e612db39084ec16ae8108/src/promoteTrayItems.cs
    /// </summary>
    public class TrayUtils
    {
        public static NotifyItemPreference? GetTrayIconShowPreference(string processName)
        {
            TrayNotify trayNotifyInstance = new TrayNotify();
            try
            {
                List<NOTIFYITEM> notifyItems = GetTrayItems(trayNotifyInstance);

                for (int i = 0; i < notifyItems.Count; i++)
                {
                    var item = notifyItems[i];

                    //string exeName = Marshal.PtrToStringAuto(item.exe_name);
                    string exeName = item.exe_name;

                    if (!string.IsNullOrEmpty(exeName) &&
                        Path.GetFileNameWithoutExtension(exeName).Equals(processName, StringComparison.OrdinalIgnoreCase))
                    {
                        return item.preference;
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(trayNotifyInstance);
            }

            return null;
        }

        public static void SetTrayIconShowPreference(string processName, NotifyItemPreference preference)
        {
            TrayNotify trayNotifyInstance = new TrayNotify();
            try
            {
                List<NOTIFYITEM> notifyItems = GetTrayItems(trayNotifyInstance);

                for (int i = 0; i < notifyItems.Count; i++)
                {
                    var item = notifyItems[i];

                    //string exeName = Marshal.PtrToStringAuto(item.exe_name);
                    string exeName = item.exe_name;
                    
                    if (!string.IsNullOrEmpty(exeName) &&
                        Path.GetFileNameWithoutExtension(exeName).Equals(processName, StringComparison.OrdinalIgnoreCase))
                    {
                        
                        SetPreference(trayNotifyInstance, item, preference);
                        return;
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(trayNotifyInstance);
            }
        }

        private class NotificationCallback : INotificationCallback
        {
            public readonly List<NOTIFYITEM> items = new List<NOTIFYITEM>();

            public void Notify([In] uint nEvent, [In] ref NOTIFYITEM notifyItem)
            {
                items.Add(notifyItem);
            }
        }

        private static bool UseLegacyInterface()
        {
            // It looks like Environment.OSVersion depends on GetVersionEx, which on its turn returns
            // different versions depending on how the application is manifested. However,
            // I do not want to rely on application manifest files, because they can be ignored,
            // if the app is 32 bit and it is targetting 64 bit windows, as shown here:
            // https://stackoverflow.com/questions/11705047/c-sharp-application-manifest-ignored-not-working
            // https://social.msdn.microsoft.com/Forums/windowsdesktop/en-US/fcebe7a4-2841-4898-9621-2e638de83fa6/application-manifest-may-be-ignored-in-windows-7-x64-ja?forum=windowscompatibility

            using (RegistryKey key = Registry.LocalMachine
               .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                var osVersion = key.GetValue("CurrentVersion");
                double version = 0d;
                if (osVersion != null && double.TryParse(osVersion.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out version))
                {
                    return version <= 6.2;
                }
            }

            // if we were unable to get the version and will assume (for now) that it is Windows 8.1 or higher
            return false;

            //var winVersion = Environment.OSVersion.Version;
            //// If the OS is Windows 8.1 or older, we should use the old interface
            //return winVersion.Major < 6 || (winVersion.Major == 6 && winVersion.Minor <= 2);
        }

        private static void SetPreference(TrayNotify trayNotifyInstance, NOTIFYITEM notifyItem, NotifyItemPreference preference)
        {
            notifyItem.preference = preference;

            var writable = NOTIFYITEM_Writable.fromNotifyItem(notifyItem);

            if (UseLegacyInterface())
            {
                ((ITrayNotifyLegacy)trayNotifyInstance).SetPreference(ref writable);
            }
            else
            {
                ((ITrayNotify)trayNotifyInstance).SetPreference(ref writable);
            }
        }
        
        private static List<NOTIFYITEM> GetTrayItems(TrayNotify trayNotifyInstance)
        {
            if (UseLegacyInterface())
            {
                var notifier = (ITrayNotifyLegacy)trayNotifyInstance;
                var callback = new NotificationCallback();

                notifier.RegisterCallback(callback);
                notifier.RegisterCallback(null);
                return callback.items;
            }
            else
            {
                var notifier = (ITrayNotify)trayNotifyInstance;
                var callback = new NotificationCallback();
                var handle = default(ulong);

                notifier.RegisterCallback(callback, out handle);
                notifier.UnregisterCallback(handle);
                return callback.items;
            }
        }
        
        // The known values for NOTIFYITEM's dwPreference member.
        public enum NotifyItemPreference
        {
            // In Windows UI: "Only show notifications."
            ShowWhenActive = 0,
            // In Windows UI: "Hide icon and notifications."
            ShowNever = 1,
            // In Windows UI: "Show icon and notifications."
            ShowAlways = 2
        };



        // NOTIFYITEM describes an entry in Explorer's registry of status icons.
        // Explorer keeps entries around for a process even after it exits.
        public struct NOTIFYITEM
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string exe_name;    // The file name of the creating executable.

            [MarshalAs(UnmanagedType.LPWStr)]
            public string tip;         // The last hover-text value associated with this status
                                       // item.

            public IntPtr icon;       // The icon associated with this status item.
            public IntPtr hwnd;       // The HWND associated with the status item.
            public NotifyItemPreference preference;  // Determines the behavior of the icon with respect to
                                                      // the taskbar
            public uint id;    // The ID specified by the application.  (hWnd, uID) is
                               // unique.
            public Guid guid;  // The GUID specified by the application, alternative to
                               // uID.
        };
        public struct NOTIFYITEM_Writable
        {
            public IntPtr exe_name;    // The file name of the creating executable.

            public IntPtr tip;         // The last hover-text value associated with this status
                                       // item.

            public IntPtr icon;       // The icon associated with this status item.
            public IntPtr hwnd;       // The HWND associated with the status item.
            public NotifyItemPreference preference;  // Determines the behavior of the icon with respect to
                                                      // the taskbar
            public uint id;    // The ID specified by the application.  (hWnd, uID) is
                               // unique.
            public Guid guid;  // The GUID specified by the application, alternative to
                               // uID.

            public static NOTIFYITEM_Writable fromNotifyItem(NOTIFYITEM item)
            {
                return new NOTIFYITEM_Writable
                {
                    exe_name = Marshal.StringToCoTaskMemAuto(item.exe_name),
                    tip = Marshal.StringToCoTaskMemAuto(item.tip),
                    icon = item.icon,
                    hwnd = item.hwnd,
                    preference = item.preference,
                    id = item.id,
                    guid = item.guid
                };
            }
        };


        [ComImport]
        [Guid("D782CCBA-AFB0-43F1-94DB-FDA3779EACCB")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface INotificationCallback
        {
            void Notify([In]uint nEvent, [In] ref NOTIFYITEM notifyItem);
        }

        [ComImport]
        [Guid("FB852B2C-6BAD-4605-9551-F15F87830935")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ITrayNotifyLegacy
        {
            void RegisterCallback([MarshalAs(UnmanagedType.Interface)]INotificationCallback callback);
            void SetPreference([In] ref NOTIFYITEM_Writable notifyItem);
            void EnableAutoTray([In] bool enabled);
        }

        [ComImport]
        [Guid("D133CE13-3537-48BA-93A7-AFCD5D2053B4")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ITrayNotify
        {
            void RegisterCallback([MarshalAs(UnmanagedType.Interface)]INotificationCallback callback, [Out] out ulong handle);
            void UnregisterCallback([In] ulong handle);
            void SetPreference([In] ref NOTIFYITEM_Writable notifyItem);
            void EnableAutoTray([In] bool enabled);
            void DoAction([In] bool enabled);
        }

        [ComImport, Guid("25DEAD04-1EAC-4911-9E3A-AD0A4AB560FD")]
        class TrayNotify { }

    }
}

