using Clipbox.Core;
using Clipbox.Win32.Native;
using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using WixSharp;
using WixSharp.Bootstrapper;

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
                ServiceInstaller = new WixSharp.ServiceInstaller
                {
                    Name = ProductConstants.SERVICE_SC_NAME,
                    Description = ProductConstants.SERVICE_SC_DESCRIPTION,
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

#if PLATFORM_X64
            ExeFileShortcut uninstaller = new ExeFileShortcut($"Uninstall {ProductConstants.PRODUCT_NAME}", "[System64Folder]msiexec.exe", "/x [ProductCode]");
#else
            ExeFileShortcut uninstaller = new ExeFileShortcut($"Uninstall {ProductConstants.PRODUCT_NAME}", "[SystemFolder]msiexec.exe", "/x [ProductCode]");
#endif

            var project =
                new ManagedProject(ProductConstants.PRODUCT_NAME,
                    new Dir(new Id("INSTALLDIR"),
                    INSTALL_DIRECTORY,
                        new Files($@"{releaseDirectory}\*", f => 
                            !f.EndsWith(ProductConstants.SERVICE_EXE_NAME)
                            && !f.EndsWith(".pdb") 
                            && !f.EndsWith(".msi")),
                        uninstaller,
                        service),
                    new Dir(@"%ProgramMenu%",
                        new ExeFileShortcut(ProductConstants.PRODUCT_NAME, System.IO.Path.Combine("[INSTALLDIR]", ProductConstants.GUARD_EXE_NAME), arguments: ProductConstants.STARTED_FROM_SHORTCUT)),
                    new ElevatedManagedAction(CustomActions.InstallDeskband, Return.check, When.Before, Step.StartServices, Condition.NOT_Installed),
                    new ElevatedManagedAction(CustomActions.UnInstallDeskband, Return.check, When.Before, Step.RemoveFiles, Condition.BeingUninstalled))
                {
                    Version = new Version(majorVersion, minorVersion),
                    GUID = new Guid(ProductConstants.PRODUCT_GUID),
                    InstallScope = InstallScope.perMachine,
                    UI = WUI.WixUI_Minimal,
                    InstallPrivileges = InstallPrivileges.elevated
                };

            project.BeforeInstall += args =>
            {
                if (args.IsUninstalling)
                {
                    System.ServiceProcess.ServiceController svc = new System.ServiceProcess.ServiceController(ProductConstants.SERVICE_SC_NAME);

                    if (svc.Status.Equals(ServiceControllerStatus.Running)
                    && !svc.Status.Equals(ServiceControllerStatus.StopPending))
                    {
                        svc.Stop();
                        svc.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0,0,10));
                    }
                }
            };

#if PLATFORM_X64
            project.Platform = Platform.x64;
#endif

            string msiPackagePath = project.BuildMsi();

            var bootstrapper =
                new Bundle(ProductConstants.PRODUCT_NAME,
                new PackageGroupRef("NetFx461Redist"),
                    // Windows 10 has .net 4.6.0 preinstalled (guaranteed)
                    //new ExePackage(@"redist\NDP472-KB4054530-x86-x64-AllOS-ENU.exe")
                    //{
                    //    Name = "Microsoft .NET Framework 4.7.2",
                    //    InstallCommand = "/passive /norestart",
                    //    Permanent = true,
                    //    Vital = true,
                    //    DetectCondition = "Netfx4FullVersion >= Netfx4FullVersion_RequiredVersion",
                    //    Compressed = true
                    //},
                    new ExePackage
                    (
#if PLATFORM_X64
                        @"redist\VC_redist.x64.exe"
#else
                        @"redist\VC_redist.x86.exe"
#endif
                    )
                    {
                        Name = "Microsoft Visual C++ Redistributable for Visual Studio 2015, 2017 and 2019.",
                        InstallCommand = $"/install /quiet /norestart /ChainingPackage \"[{ProductConstants.PRODUCT_NAME}]\"",
                        Permanent = true,
                        Vital = true,
                        PerMachine = true,
#if PLATFORM_X64
                        DetectCondition = "VCPlusPlus2015_x64 >= VCPlusPlus2015_x64_RequiredVersion",
#else
                        DetectCondition = "VCPlusPlus2015_x86 >= VCPlusPlus2015_x86_RequiredVersion",
#endif
                        Compressed = true
                    },
                    new MsiPackage(msiPackagePath)
                    {
                        DisplayInternalUI = false,
                        Visible = false,
                        Vital = true
                    })
                {
                    Version = new Version(majorVersion, minorVersion),
                    Manufacturer = ProductConstants.COMPANY_NAME,
                    UpgradeCode = new Guid(ProductConstants.WIX_BUNDLE_UPGRADE_CODE),
                    OutFileName = $"{ProductConstants.PRODUCT_NAME}_Setup"
                };

            bootstrapper.Include(WixExtension.Util);

            bootstrapper.IncludeWixExtension(@"WixDependencyExtension.dll", "dep", "http://schemas.microsoft.com/wix/DependencyExtension");
            bootstrapper.AttributesDefinition = "{dep}ProviderKey=01234567-8901-2345-6789-012345678901";
            bootstrapper.AddWixFragment("Wix/Bundle",
                                    new UtilRegistrySearch
                                    {
                                        Root = RegistryHive.LocalMachine,
                                        Key = @"SOFTWARE\Microsoft\Net Framework Setup\NDP\v4\Full",
                                        Value = "Version",
                                        Variable = "Netfx4FullVersion"
                                    },
                                    new UtilProductSearch
                                    {
                                     UpgradeCode = "{36F68A90-239C-34DF-B58C-64B30153CE35}",
                                     Variable = "VCPlusPlus2015_x64"
                                    },
                                    new UtilProductSearch
                                    {
                                        UpgradeCode = "{65E5BD06-6392-3027-8C26-853107D3CF1A}",
                                        Variable = "VCPlusPlus2015_x86"
                                    });

            bootstrapper.Variables = new[]
            {
                new Variable("VCPlusPlus2015_x64_RequiredVersion")
                {
                         Type = WixSharp.VariableType.version,
                         Value = "14.27.29112"
                },
                new Variable("VCPlusPlus2015_x86_RequiredVersion")
                {
                         Type = WixSharp.VariableType.version,
                         Value = "14.27.29112"
                },
                new Variable("Netfx4FullVersion_RequiredVersion")
                {
                         Type = WixSharp.VariableType.version,
                         Value = "4.7.2"
                }
            };

            string finalSetup = bootstrapper.Build();
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

                    string deskbandInstallErrorFlagFilePath = System.IO.Path.Combine(session.Property("INSTALLDIR"), ProductConstants.DESKBAND_INSTALL_FAILURE_FLAG_FILENAME);
                    if (System.IO.File.Exists(deskbandInstallErrorFlagFilePath))
                    {
                        System.IO.File.Delete(deskbandInstallErrorFlagFilePath);
                    }
                });
            }

            [CustomAction]
            public static ActionResult StopService(Session session)
            {
                return session.HandleErrors(() =>
                {
                    ServiceController service = new ServiceController(ProductConstants.SERVICE_SC_NAME);

                    if (service.Status.Equals(ServiceControllerStatus.Running)
                    && !service.Status.Equals(ServiceControllerStatus.StopPending))
                    {
                        service.Stop();
                    }
                });
            }
           
        }
    }
}
