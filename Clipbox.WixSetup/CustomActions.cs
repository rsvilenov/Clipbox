using Microsoft.Deployment.WindowsInstaller;
using Setup.WixSetup;
using System.Diagnostics;
using System.ServiceProcess;
using WixSharp;

namespace Clipbox.WixSetup
{
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

        [CustomAction]
        public static ActionResult SetInitialCultureCode(Session session)
        {
            return session.HandleErrors(() =>
            {
                string selectedCultureCode = session.Property("INITIAL_CULTURE_CODE");
                if (string.IsNullOrEmpty(selectedCultureCode))
                {
                    selectedCultureCode = "en-US";
                }

                session.Log("Selected culture code to be set to the installed program: {0}", selectedCultureCode);

                string cultureCodeFilePath = System.IO.Path.Combine(session.Property("INSTALLDIR"), ProductConstants.INITIAL_CULTURE_CODE_FILENAME);
                System.IO.File.WriteAllText(cultureCodeFilePath, selectedCultureCode);
            });
        }

    }
}
