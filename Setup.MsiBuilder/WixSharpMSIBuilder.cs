using Clipbox.Core;
using Clipbox.Win32.Native;
using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Diagnostics;
using WixSharp;

namespace Setup.MsiBuilder
{
    public static class WixSharpMSIBuilder
    {
        public static void CreateMSI()
        {
            int majorVersion = (int)Math.Floor(ProductConstants.PRODUCT_VERSION);
            int minorVersion = (int)ProductConstants.PRODUCT_VERSION % 1;
#if DEBUG
            string releaseDirectory = @"..\Build\Debug\";
#elif RELEASE
            string releaseDirectory = @"..\Build\Release\"
#endif
            string installDirectory = $@"%ProgramFiles%\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";

            File service = new File(System.IO.Path.Combine(releaseDirectory, ProductConstants.SERVICE_EXE_NAME))
            {
                ServiceInstaller = new ServiceInstaller
                {
                    Name = $"{ProductConstants.PRODUCT_NAME}.Svc",
                    StartOn = SvcEvent.Install,
                    StopOn = SvcEvent.InstallUninstall_Wait,
                    RemoveOn = SvcEvent.Uninstall_Wait,
                    FirstFailureActionType = FailureActionType.restart,
                    SecondFailureActionType = FailureActionType.restart,
                    ThirdFailureActionType = FailureActionType.none,
                    DelayedAutoStart = false
                }
            };

            File guard = new File(System.IO.Path.Combine(releaseDirectory, ProductConstants.GUARD_EXE_NAME),
                            new FileShortcut(ProductConstants.PRODUCT_NAME, $@"%ProgramMenu%\{ProductConstants.PRODUCT_NAME}"));

            var project =
                new Project(ProductConstants.PRODUCT_NAME,
                    new Dir(new Id("INSTALLDIR"),
                    installDirectory,
                    new Files($@"{releaseDirectory}\*", excludePath => !excludePath.EndsWith(ProductConstants.GUARD_EXE_NAME) && !excludePath.EndsWith(ProductConstants.SERVICE_EXE_NAME)), service, guard),
                    new ElevatedManagedAction(CustomActions.InstallDeskband, Return.check, When.After, Step.InstallFiles, Condition.NOT_Installed),
                    new ElevatedManagedAction(CustomActions.UnInstallDeskband, Return.check, When.Before, Step.RemoveFiles, Condition.BeingUninstalled))
                {
                    Version = new Version(majorVersion, minorVersion),
                    GUID = new Guid(ProductConstants.PRODUCT_GUID),
                    InstallScope = InstallScope.perMachine
                };

#if PLATFORM_X64
            project.Platform = Platform.x64;
#endif

            project.BuildMsi();
        }

        public static class CustomActions
        {
            [CustomAction]
            public static ActionResult InstallDeskband(Session session)
            {
                return session.HandleErrors(() =>
                {
                    var dll = System.IO.Path.Combine(session.Property("INSTALLDIR"), "ClipboxDeskband.dll");
                    Process.Start("regsvr32.exe", $"/s \"{dll}\"").WaitForExit();

                    // notify Windows Explorer for the change, so that the extension can be loaded immediately
                    Shell32.SHChangeNotify(NativeEnums.SHCNE.SHCNE_ASSOCCHANGED, NativeEnums.SHChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                });
            }

            [CustomAction]
            public static ActionResult UnInstallDeskband(Session session)
            {
                return session.HandleErrors(() =>
                {
                    var dll = System.IO.Path.Combine(session.Property("INSTALLDIR"), "ClipboxDeskband.dll");
                    Process.Start("regsvr32.exe", $"/s /u \"{dll}\"").WaitForExit();

                    // notify Windows Explorer for the change
                    Shell32.SHChangeNotify(NativeEnums.SHCNE.SHCNE_ASSOCCHANGED, NativeEnums.SHChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                });
            }
        }
    }
}
