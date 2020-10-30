using System;
using System.Reflection;
using System.ServiceProcess;
using WixSharp;
using WixSharp.Bootstrapper;

// let Visual Studio auto-increment the build and revision numbers.
// In order for this to work, in the csprj file "Deterministic" should be set to "false"
[assembly: AssemblyVersion("1.2.*")]

namespace Clipbox.WixSetup
{
    public class WixBundleBuilder
    {
        //The UI implementation is based on the work of BRYANPJOHNSTON
        //http://bryanpjohnston.com/2012/09/28/custom-wix-managed-bootstrapper-application/
        private static readonly string INSTALL_DIRECTORY = $@"%ProgramFiles%\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";

        static public void Setup()
        {
            // this assembly version will be our product full version.
            Version asmVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            if (double.Parse($"{asmVersion.Major}.{asmVersion.Minor}") != ProductConstants.PRODUCT_VERSION)
            {
                throw new InvalidOperationException($"Assebmly's major.minor version is {asmVersion.Major}.{asmVersion.Minor} while the product version is {ProductConstants.PRODUCT_VERSION}. They should match.");
            }
            
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

            File mainProcessFile = new File(System.IO.Path.Combine(releaseDirectory, ProductConstants.MAIN_PROCESS_EXE_NAME))
            {
                FirewallExceptions = new FirewallException[]
                {
                    new FirewallException
                    {
                        Name = $"{ProductConstants.PRODUCT_NAME} Main Process",
                        Scope = FirewallExceptionScope.any,
                        Profile = FirewallExceptionProfile.all
                    }
                }
            };

            File guardProcessFile = new File(System.IO.Path.Combine(releaseDirectory, ProductConstants.GUARD_EXE_NAME))
            {
                FirewallExceptions = new FirewallException[]
                {
                    new FirewallException
                    {
                        Name = $"{ProductConstants.PRODUCT_NAME} Guard Process",
                        Scope = FirewallExceptionScope.any,
                        Profile = FirewallExceptionProfile.all
                    }
                }
            };

#if PLATFORM_X64
            string pathToMsiExec = "[System64Folder]msiexec.exe";
#else
            string pathToMsiExec = "[SystemFolder]msiexec.exe";
#endif

            ExeFileShortcut uninstaller = new ExeFileShortcut($"Uninstall {ProductConstants.PRODUCT_NAME}", pathToMsiExec, "/x [ProductCode]");

            var project =
                new ManagedProject(ProductConstants.PRODUCT_NAME,
                    new Dir(new Id("INSTALLDIR"),
                        new Files($@"{releaseDirectory}\*", f =>
                            !f.EndsWith(ProductConstants.SERVICE_EXE_NAME)
                            && !f.EndsWith(ProductConstants.MAIN_PROCESS_EXE_NAME)
                            && !f.EndsWith(ProductConstants.GUARD_EXE_NAME)
                            && !f.EndsWith(".pdb")
                            && !f.EndsWith(".msi")),
                        mainProcessFile,
                        guardProcessFile,
                        uninstaller,
                        service),
                    new Dir(@"%ProgramMenu%",
                        new ExeFileShortcut(ProductConstants.PRODUCT_NAME, System.IO.Path.Combine("[INSTALLDIR]", ProductConstants.GUARD_EXE_NAME), arguments: ProductConstants.STARTED_FROM_SHORTCUT)),
                    new Dir(@"%Desktop%",
                        new ExeFileShortcut(ProductConstants.PRODUCT_NAME, System.IO.Path.Combine("[INSTALLDIR]", ProductConstants.GUARD_EXE_NAME), arguments: ProductConstants.STARTED_FROM_SHORTCUT)
                        {
                            ComponentCondition = new Condition("DESKTOP_SHORTCUT=\"yes\"")
                        }),
                    new ElevatedManagedAction(CustomActions.InstallDeskband, Return.check, When.Before, Step.StartServices, WixSharp.Condition.NOT_Installed)
                    {
                        UsesProperties = "PIN_TRAY_ICON=[PIN_TRAY_ICON]"
                    },
                    new ElevatedManagedAction(CustomActions.UnInstallDeskband, Return.check, When.Before, Step.RemoveFiles, WixSharp.Condition.BeingUninstalled),
                    new ElevatedManagedAction(CustomActions.CleanUpArtefacts, Return.ignore, When.After, Step.RemoveFiles, WixSharp.Condition.BeingUninstalledAndNotBeingUpgraded),
                    new RegValue(RegistryHive.LocalMachine, $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}", "InstallDirectory", "[INSTALLDIR]")
                    {
#if PLATFORM_X64
                        Win64 = true
#endif
                    },
                    new RegValue(RegistryHive.LocalMachine, $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}", "InitialCultureCode", "[INITIAL_CULTURE_CODE]")
                    {
#if PLATFORM_X64
                        Win64 = true
#endif
                    },
                    new RegValue(RegistryHive.LocalMachine, $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}", "InitialPinTrayIcon", "[PIN_TRAY_ICON]")
                    {
#if PLATFORM_X64
                        Win64 = true
#endif
                    },
                    new RegValue(RegistryHive.LocalMachine, $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}", "ProductVersion", asmVersion.ToString())
                    {
#if PLATFORM_X64
                        Win64 = true
#endif
                    })
                {
                    Version = asmVersion,
                    GUID = new Guid(ProductConstants.MSI_PRODUCT_GUID),
                    InstallScope = InstallScope.perMachine,
                    UI = WUI.WixUI_Minimal,
                    InstallPrivileges = InstallPrivileges.elevated
                };

            // add a default install dir for the msi package
            // it will be overridden by the bundle...
            project.WixVariables.Add("INSTALLDIR", INSTALL_DIRECTORY);

            project.WixVariables.Add("INITIAL_CULTURE_CODE", "en-US");
            project.WixVariables.Add("DESKTOP_SHORTCUT", "no");
            project.WixVariables.Add("PIN_TRAY_ICON", "yes");

            project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;
            project.MajorUpgradeStrategy.RemoveExistingProductAfter = Step.InstallInitialize;

            project.BeforeInstall += args =>
            {
                if (args.IsUninstalling || args.IsRepairing || args.IsUpgrading)
                {
                    System.ServiceProcess.ServiceController svc = new System.ServiceProcess.ServiceController(ProductConstants.SERVICE_SC_NAME);

                    if (svc.Status.Equals(ServiceControllerStatus.Running)
                    && !svc.Status.Equals(ServiceControllerStatus.StopPending))
                    {
                        svc.Stop();
                        svc.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 10));
                    }
                }
            };

#if PLATFORM_X64
            project.Platform = Platform.x64;
#endif

            string msiPackagePath = project.BuildMsi();


            var bootstrapper =
                new Bundle(ProductConstants.PRODUCT_NAME,
                new PackageGroupRef("NetFx461Web"),
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
                        Id = ProductConstants.MSI_PACKAGE_ID,
                        DisplayInternalUI = false,
                        Visible = false,
                        Vital = true,
                        // pass the custom install dir property to the MSI package.
                        // this property is defined in bootstrapper.Variables below.
                        MsiProperties = "INSTALLDIR=[ACTUAL_INSTALLDIR];INITIAL_CULTURE_CODE=[ACTUAL_INITIAL_CULTURE_CODE];DESKTOP_SHORTCUT=[CREATE_DESKTOP_SHORTCUT];PIN_TRAY_ICON=[PIN_TRAY_ICON]"
                    })
                {
                    Version = asmVersion,
                    Manufacturer = ProductConstants.COMPANY_NAME,
                    UpgradeCode = new Guid(ProductConstants.WIX_BUNDLE_UPGRADE_CODE),
                    OutFileName = ProductConstants.PRODUCT_NAME
                };

            bootstrapper.Include(WixExtension.Util);
            bootstrapper.SuppressWixMbaPrereqVars = true;

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
                // make it known that we will use a custom property for the install dir
                // and set its value to the default location. It can be overridden by the UI.
                new Variable ("ACTUAL_INSTALLDIR", INSTALL_DIRECTORY),

                new Variable("ACTUAL_INITIAL_CULTURE_CODE", "en-US"),
                new Variable("CREATE_DESKTOP_SHORTCUT", "no"),
                new Variable("PIN_TRAY_ICON", "yes"),

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
            bootstrapper.PreserveTempFiles = true;
            bootstrapper.Application = new ManagedBootstrapperApplication("%this%", "BootstrapperCore.config");

            string finalSetup = bootstrapper.Build();
        }
    }
}
