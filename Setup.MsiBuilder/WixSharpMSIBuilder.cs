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
        private static readonly string INSTALL_DIRECTORY = $@"%ProgramFiles%\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";
        public static void CreateMSI()
        {
            int majorVersion = (int)Math.Floor(ProductConstants.PRODUCT_VERSION);
            int minorVersion = (int)ProductConstants.PRODUCT_VERSION % 1;
#if DEBUG
            string releaseDirectory = @"..\Build\Debug\";
#elif RELEASE
            string releaseDirectory = @"..\Build\Release\"
#endif
            

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
                    DelayedAutoStart = false,
                    Vital = true
                }
            };

            File guard = new File(System.IO.Path.Combine(releaseDirectory, ProductConstants.GUARD_EXE_NAME),
                            new FileShortcut(ProductConstants.PRODUCT_NAME, $@"%ProgramMenu%\{ProductConstants.PRODUCT_NAME}"));

#if PLATFORM_X64
            ExeFileShortcut uninstaller = new ExeFileShortcut($"Uninstall {ProductConstants.PRODUCT_NAME}", "[System64Folder]msiexec.exe", "/x [ProductCode]");
#else
            ExeFileShortcut uninstaller = new ExeFileShortcut($"Uninstall {ProductConstants.PRODUCT_NAME}", "[SystemFolder]msiexec.exe", "/x [ProductCode]");
#endif

            var project =
                new Project(ProductConstants.PRODUCT_NAME,
                    new Dir(new Id("INSTALLDIR"),
                    INSTALL_DIRECTORY,
                        new Files($@"{releaseDirectory}\*", f => 
                            !f.EndsWith(ProductConstants.GUARD_EXE_NAME) 
                            && !f.EndsWith(ProductConstants.SERVICE_EXE_NAME)
                            && !f.EndsWith(".pdb") 
                            && !f.EndsWith(".msi")),
                        guard,
                        uninstaller,
                        service),
                    new ElevatedManagedAction(CustomActions.InstallDeskband, Return.check, When.Before, Step.StartServices, Condition.NOT_Installed),
                    new ElevatedManagedAction(CustomActions.UnInstallDeskband, Return.check, When.Before, Step.RemoveFiles, Condition.BeingUninstalled))
                {
                    Version = new Version(majorVersion, minorVersion),
                    GUID = new Guid(ProductConstants.PRODUCT_GUID),
                    InstallScope = InstallScope.perMachine,
                    UI = WUI.WixUI_Minimal,
                    InstallPrivileges = InstallPrivileges.elevated
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
                    var dll = System.IO.Path.Combine(session.Property("INSTALLDIR"), ProductConstants.DESKBAND_SHELL_EXT_DLL_NAME);
                    Process.Start("regsvr32.exe", $"/s \"{dll}\"").WaitForExit();

                    var deskbandCtrlExe = System.IO.Path.Combine(session.Property("INSTALLDIR"), ProductConstants.DESKBAND_CTRL_EXE_NAME);
                    using (Process deskbandCtlProcess = Process.Start(deskbandCtrlExe, "nowait"))
                    {
                        deskbandCtlProcess.WaitForExit();
                        if (deskbandCtlProcess.ExitCode != 0)
                        {
                            System.IO.File.Create(System.IO.Path.Combine(session.Property("INSTALLDIR"), ProductConstants.DESKBAND_INSTALL_FAILURE_FLAG_FILENAME));
                        }
                    }
                });
            }

            [CustomAction]
            public static ActionResult UnInstallDeskband(Session session)
            {
                return session.HandleErrors(() =>
                {
                    var dll = System.IO.Path.Combine(session.Property("INSTALLDIR"), ProductConstants.DESKBAND_SHELL_EXT_DLL_NAME);
                    Process.Start("regsvr32.exe", $"/s /u \"{dll}\"").WaitForExit();
                });
            }
        }
    }
}
