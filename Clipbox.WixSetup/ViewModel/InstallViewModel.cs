using Clipbox.WixSetup.Model;
using Clipbox.WixSetup.ViewModel.Enums;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Microsoft.Win32;
using Setup.WixSetup.ViewModel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Clipbox.WixSetup.ViewModel
{
    public class InstallViewModel : BindableObject
    {
        private readonly BootstrapperApplicationModel _model;
        private readonly object _lockHandle = new object();

        public event EventHandler<CancelEventArgs> QuitConfirmationRequested;
        public event EventHandler<CancelEventArgs> CancelConfirmationRequested;
        public event EventHandler<CancelEventArgs> FactoryResetConfirmationRequested;
        public event EventHandler<CancelEventArgs> UninstallConfirmationRequested;
        public event EventHandler DetectComplete;

        public ICommand QuitCommand { get; set; }
        public ICommand CloseInstallWindowCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand InstallCommand { get; set; }
        public ICommand UninstallCommand { get; set; }
        public ICommand UpdateCommand { get; set; }
        public ICommand UpdateDownloadCommand { get; set; }
        public ICommand RepairCommand { get; set; }
        public ICommand ModifyCommand { get; set; }
        public ICommand FactoryResetCommand { get; set; }
        public ICommand RetryCommand { get; set; }

        private bool _isCancelRequested;
        private bool _isSetupActionRequested;
        private int _cacheProgress;
        private int _executeProgress;
        private LaunchAction? _lastLaunchAction;
        private Version _installedVersion;

        public InstallViewModel(BootstrapperApplicationModel model)
        {
            _model = model;

            InstallationDirectory =
                System.IO.Path.Combine(FindProperProgramFilesDir(), ProductConstants.COMPANY_NAME, ProductConstants.PRODUCT_NAME);

            QuitCommand = new DelegateCommand(QuitCommandHandler);
            CloseInstallWindowCommand = new DelegateCommand(CloseInstallWindowCommandHandler);
            CancelCommand = new DelegateCommand(CancelCommandHandler, () => !_isCancelRequested);
            InstallCommand = new DelegateCommand(() => RequestSetupAction(LaunchAction.Install), () => !_isSetupActionRequested);
            UninstallCommand = new DelegateCommand(() => UninstallCommandHandler(), () => !_isSetupActionRequested);
            RepairCommand = new DelegateCommand(() => RequestSetupAction(LaunchAction.Repair), () => !_isSetupActionRequested);
            UpdateCommand = new DelegateCommand(() => RequestSetupAction(LaunchAction.UpdateReplace), () => !_isSetupActionRequested);
            UpdateDownloadCommand = new DelegateCommand(UpdateDownloadCommandHandler, () => !_isSetupActionRequested);
            ModifyCommand = new DelegateCommand(ModifyCommandHandler, ModifyCommandHandlerCanExecuteHandler);
            FactoryResetCommand = new DelegateCommand(FactoryResetCommandHandler, () => !_isSetupActionRequested);
            RetryCommand = new DelegateCommand(RetryCommandHandler, () => !_isSetupActionRequested && _lastLaunchAction != null);

            _model.BootstrapperApplication.DetectBegin += BootstrapperApplication_DetectBegin;
            _model.BootstrapperApplication.DetectComplete += BootstrapperApplication_DetectComplete;
            _model.BootstrapperApplication.DetectPackageComplete += BootstrapperApplication_DetectPackageComplete;
            _model.BootstrapperApplication.DetectRelatedBundle += BootstrapperApplication_DetectRelatedBundle;
            _model.BootstrapperApplication.DetectRelatedMsiPackage += BootstrapperApplication_DetectRelatedMsiPackage;
            _model.BootstrapperApplication.PlanBegin += BootstrapperApplication_PlanBegin;
            _model.BootstrapperApplication.PlanComplete += BootstrapperApplication_PlanComplete;
            _model.BootstrapperApplication.ApplyBegin += BootstrapperApplication_ApplyBegin;
            _model.BootstrapperApplication.ApplyComplete += BootstrapperApplication_ApplyComplete;
            _model.BootstrapperApplication.Error += BootstrapperApplication_Error;
            _model.BootstrapperApplication.CacheAcquireProgress += BootstrapperApplication_CacheAcquireProgress;
            _model.BootstrapperApplication.ExecuteProgress += BootstrapperApplication_ApplyExecuteProgress;
            _model.BootstrapperApplication.Progress += BootstrapperApplication_Progress;
            _model.BootstrapperApplication.ExecuteMsiMessage += BootstrapperApplication_ExecuteMsiMessage;
            _model.BootstrapperApplication.ExecutePackageBegin += BootstrapperApplication_ExecutePackageBegin;
            _model.BootstrapperApplication.ExecutePackageComplete += BootstrapperApplication_ExecutePackageComplete;

        }

        private string FindProperProgramFilesDir()
        {
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                return Environment.GetEnvironmentVariable("ProgramW6432");
            }
            else
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }
        }

        private bool ModifyCommandHandlerCanExecuteHandler()
        {
            try
            {
                return CheckIfGuardProcessIsRunning();
            }
            catch (Exception ex)
            {
                try { _model.BootstrapperApplication.Engine.Log(LogLevel.Error, ex.Message); } catch { }
                return false;
            }
        }

        private void ModifyCommandHandler()
        {
            _isSetupActionRequested = true;

            try
            {
                CheckIfGuardProcessIsRunning();

                string installDir = GetInstallDirFromRegistry();
                if (installDir == null)
                {
                    throw new System.IO.DirectoryNotFoundException("Install directory not found in registry.");
                }
                string guardPath = System.IO.Path.Combine(installDir, ProductConstants.GUARD_EXE_NAME);
                Process.Start(guardPath, ProductConstants.OPEN_SETTINGS_WINDOW_ARG);
                QuitInstallerInternal(0);
            }
            catch (Exception ex)
            {
                SetupState = ClipboxSetupState.Error;
                ErrorMessage = ex.Message;
            }
            finally
            {
                _isSetupActionRequested = false;
            }
        }

        private string GetInstallDirFromRegistry()
        {
            string productSubKey = $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";
            string installDir = null;

            using (RegistryKey HKLM32 =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        RegistryView.Registry32))
            using (RegistryKey key = HKLM32.OpenSubKey(productSubKey))
            {
                installDir = key?.GetValue("InstallDirectory") as string;
            }

            if (installDir == null)
            {
                using (RegistryKey HKLM64 =
                       RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                           RegistryView.Registry64))
                using (RegistryKey key = HKLM64.OpenSubKey(productSubKey))
                {
                    installDir = key?.GetValue("InstallDirectory") as string;
                }
            }

            return installDir;
        }

        private string GetCultureCodeFromRegistry()
        {
            string productSubKey = $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";
            string cultureCode = null;

            using (RegistryKey HKCU32 =
                    RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,
                        RegistryView.Registry32))
            using (RegistryKey key = HKCU32.OpenSubKey(productSubKey))
            {
                cultureCode = key?.GetValue("CultureCode") as string;
            }

            if (cultureCode == null)
            {
                using (RegistryKey HKCU64 =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        RegistryView.Registry64))
                using (RegistryKey key = HKCU64.OpenSubKey(productSubKey))
                {
                    cultureCode = key?.GetValue("CultureCode") as string;
                }
            }

            if (cultureCode == null)
            {
                using (RegistryKey HKLM32 =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        RegistryView.Registry32))
                using (RegistryKey key = HKLM32.OpenSubKey(productSubKey))
                {
                    cultureCode = key?.GetValue("InitialCultureCode") as string;
                }

                if (cultureCode == null)
                {
                    using (RegistryKey HKLM64 =
                        RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                            RegistryView.Registry64))
                    using (RegistryKey key = HKLM64.OpenSubKey(productSubKey))
                    {
                        cultureCode = key?.GetValue("InitialCultureCode") as string;
                    }
                }
            }

            return cultureCode;
        }

        private bool CheckIfGuardProcessIsRunning()
        {
            int mySessionId = Process.GetCurrentProcess().SessionId;
            Process[] guardProcs = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(ProductConstants.GUARD_EXE_NAME));
            foreach (Process p in guardProcs)
            {
                if (p.SessionId == mySessionId)
                {
                    return true;
                }
            }

            return false;
        }

        private void FactoryResetCommandHandler()
        {
            _isSetupActionRequested = true;

            try
            {
                Task.Factory.StartNew(() =>
                {
                    CancelEventArgs args = new CancelEventArgs();
                    FactoryResetConfirmationRequested?.Invoke(this, args);
                    if (!args.Cancel)
                    {
                        ModifyAction = ClipboxModifyAction.FactoryReset;
                        SetupState = ClipboxSetupState.Processing;

                        // some progress values...
                        Progress = 15;

                        try
                        {
                            ServiceController service = new ServiceController(ProductConstants.SERVICE_SC_NAME);

                            bool wasServiceRunning = false;
                            if (service.Status.Equals(ServiceControllerStatus.Running)
                            && !service.Status.Equals(ServiceControllerStatus.StopPending))
                            {
                                wasServiceRunning = true;
                                service.Stop(); 
                                service.WaitForStatus(ServiceControllerStatus.Stopped);
                            }

                            Progress = 35;
                            
                            CleanUpUtility.CleanUp(CleanUpUtility.CleanUpType.AppDataSettings, justCurrentUser: true);

                            Progress = 75;

                            if (wasServiceRunning)
                            {
                                service.Start();
                                service.WaitForStatus(ServiceControllerStatus.Running);
                            }

                            Progress = 100;

                            SetupState = ClipboxSetupState.Done;
                        }
                        catch (Exception ex)
                        {
                            SetupState = ClipboxSetupState.Error;
                            ErrorMessage = ex.Message;
                            try { _model.LogMessage(ex.ToString()); } catch { }
                        }
                    }
                });
            }
            finally
            {
                _isSetupActionRequested = false;
            }
        }


        /// <summary>
        /// This command causes unpredictable results.
        /// </summary>
        private void CancelCommandHandler()
        {
            _isSetupActionRequested = true;

            try
            {
                Task.Factory.StartNew(() =>
                {
                    CancelEventArgs args = new CancelEventArgs();
                    CancelConfirmationRequested?.Invoke(this, args);
                    if (!args.Cancel)
                    {
                        _isCancelRequested = true;
                    }
                });
            }
            finally
            {
                _isSetupActionRequested = false;
            }
        }

        private void RetryCommandHandler()
        {
            if (_lastLaunchAction.HasValue)
            {
                SetupState = ClipboxSetupState.Preparing;
                RequestSetupAction(_lastLaunchAction.Value);
            }
            else
            {
                SetupState = ClipboxSetupState.Error;
            }
        }

        private void UninstallCommandHandler()
        {
            _isSetupActionRequested = true;

            try
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        CancelEventArgs args = new CancelEventArgs();
                        UninstallConfirmationRequested?.Invoke(this, args);
                        if (args.Cancel)
                        {
                            return;
                        }

                        ModifyAction = ClipboxModifyAction.Uninstall;

                        _model.PlanAction(LaunchAction.Uninstall);
                    }
                    catch (Exception ex)
                    {
                        try { _model.LogMessage(ex.ToString()); } catch { }
                        SetupState = ClipboxSetupState.Error;
                        ErrorMessage = ex.Message;
                    }
                });
            }
            finally
            {
                _isSetupActionRequested = false;
            }
        }

        private void RequestSetupAction(LaunchAction launchAction)
        {
            _isSetupActionRequested = true;

            try
            {
                _lastLaunchAction = launchAction;

                switch (launchAction)
                {
                    case LaunchAction.Repair:
                        ModifyAction = ClipboxModifyAction.Repair;
                        break;
                    case LaunchAction.Uninstall:
                        throw new InvalidOperationException($"Uninstalling should be handled by {nameof(UninstallCommandHandler)}");
                    case LaunchAction.Modify:
                        ModifyAction = ClipboxModifyAction.Modify;
                        break;
                    case LaunchAction.Install:
                        SetBurnVariables();
                        break;
                }

                _model.PlanAction(launchAction);

            }
            finally
            {
                _isSetupActionRequested = false;
            }
        }

        private void SetBurnVariables()
        {
            _model.SetBurnVariable("CREATE_DESKTOP_SHORTCUT", CreateDesktopShortcut ? "yes" : "no");
            _model.SetBurnVariable("PIN_TRAY_ICON", PinTrayIcon ? "yes" : "no");
            _model.SetBurnVariable("ACTUAL_INSTALLDIR", InstallationDirectory);
            _model.SetBurnVariable("ACTUAL_INITIAL_CULTURE_CODE", CultureCode);
        }

        private void UpdateDownloadCommandHandler()
        {
            _isSetupActionRequested = true;

            try
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        ModifyAction = ClipboxModifyAction.DownloadUpdate;
                        SetupState = ClipboxSetupState.Downloading;

                        string tempFilePath = System.IO.Path.GetTempFileName();

                        WebClient webClient = new WebClient();
                        webClient.DownloadProgressChanged += (s, e) =>
                        {
                            Progress = e.ProgressPercentage;
                        };
                        webClient.DownloadFileCompleted += (s, e) =>
                        {
                            System.IO.File.Move(tempFilePath, $"{tempFilePath}.exe");

                            Process.Start($"{tempFilePath}.exe");
                            QuitInstallerInternal(0);
                        };

                        webClient.DownloadFileAsync(new Uri(ProductConstants.PRODUCT_UPDATE_URL),
                            tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        SetupState = ClipboxSetupState.Error;
                        ErrorMessage = ex.Message;
                    }
                });
            }
            finally
            {
                _isSetupActionRequested = false;
            }
        }

        private void BootstrapperApplication_Error(object sender, ErrorEventArgs e)
        {
            SetupState = ClipboxSetupState.Error;
            ErrorMessage = e.ErrorMessage;
        }

        private void BootstrapperApplication_ExecuteMsiMessage(object sender, ExecuteMsiMessageEventArgs e)
        {
            lock (_lockHandle)
            {
                if (e.MessageType == Microsoft.Tools.WindowsInstallerXml.Bootstrapper.InstallMessage.ActionStart)
                {
                    Message = e.Message;
                }

                e.Result = _isCancelRequested ? Result.Cancel : Result.Ok;
            }
        }

        private void BootstrapperApplication_ExecutePackageBegin(object sender, ExecutePackageBeginEventArgs e)
        {
            if (_isCancelRequested)
            {
                e.Result = Result.Cancel;
            }
        }

        private void BootstrapperApplication_ExecutePackageComplete(object sender, ExecutePackageCompleteEventArgs e)
        {
            if (_isCancelRequested)
            {
                e.Result = Result.Cancel;
            }
        }


        private void BootstrapperApplication_ApplyComplete(object sender, ApplyCompleteEventArgs e)
        {
            IsRestartRequired = e.Restart == ApplyRestart.RestartRequired;
            _model.FinalResult = e.Status;
            if (SetupState != ClipboxSetupState.Error)
            {
                SetupState = ClipboxSetupState.Done;
            }
        }

        private void BootstrapperApplication_ApplyBegin(object sender, ApplyBeginEventArgs e)
        {
            if (SetupState != ClipboxSetupState.Error)
            {
                SetupState = ClipboxSetupState.Processing;
            }
        }

        private void BootstrapperApplication_PlanBegin(object sender, PlanBeginEventArgs e)
        {
            Phases = (LaunchAction.Layout == _model.PlannedAction) ? 1 : 2;
        }

        private void BootstrapperApplication_PlanComplete(object sender, PlanCompleteEventArgs e)
        {
            if (_isCancelRequested)
            {
                QuitInstallerInternal(0);
                return;
            }

            if (SetupState != ClipboxSetupState.Error)
            {
                SetupState = ClipboxSetupState.Processing;
            }

            _model.ApplyAction();
        }

        private void BootstrapperApplication_DetectRelatedMsiPackage(object sender, DetectRelatedMsiPackageEventArgs e)
        {
            _installedVersion = e.Version;
        }

        private void BootstrapperApplication_DetectRelatedBundle(object sender, DetectRelatedBundleEventArgs e)
        {
            _installedVersion = e.Version;
        }

        private void BootstrapperApplication_DetectPackageComplete(object sender, Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectPackageCompleteEventArgs e)
        {
            if (e.PackageId.Equals(ProductConstants.MSI_PACKAGE_ID, StringComparison.Ordinal))
            {
                if (_model.BootstrapperApplication.Command.GetCommandLineArgs().Any(arg => arg.Replace("/", "").Equals(ProductConstants.FORCE_INSTALL_ARG, StringComparison.OrdinalIgnoreCase)))
                {
                    SetupWorkflow = ClipboxSetupWorkflow.Install;
                }
                else if (e.State == PackageState.Present)
                {
                    SetupWorkflow = ClipboxSetupWorkflow.Modify;
                }
                else
                {
                    Version bundleVersion = _model.BootstrapperApplication.Engine.VersionVariables["WixBundleVersion"];
                    if (_installedVersion > bundleVersion)
                    {
                        // newer version alredy installed
                        IsNewerVersionAlreadyInstalled = true;
                    }
                    else if (_installedVersion < bundleVersion)
                    {
                        SetupWorkflow = ClipboxSetupWorkflow.Update;
                    }
                    else if (_installedVersion == bundleVersion)
                    {
                        SetupWorkflow = ClipboxSetupWorkflow.Modify;
                    }
                    else
                    {
                        SetupWorkflow = ClipboxSetupWorkflow.Install;
                    }
                }
            }
        }

        private void BootstrapperApplication_DetectBegin(object sender, Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectBeginEventArgs e)
        {
            IsDetectionOngoing = true;
            if (SetupState != ClipboxSetupState.Error)
            {
                SetupState = ClipboxSetupState.Loading;
            }
        }

        private void BootstrapperApplication_DetectComplete(object sender, Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectCompleteEventArgs e)
        {
            IsDetectionOngoing = false;

            if (SetupWorkflow == ClipboxSetupWorkflow.Modify 
                || SetupWorkflow == ClipboxSetupWorkflow.Update)
            {
                try
                {
                    CultureCode = GetCultureCodeFromRegistry();
                }
                catch (Exception ex)
                {
                    try { _model.LogMessage($"Cannot get the culture code from registry: {ex}"); } catch { }
                }
            }

            if (SetupState != ClipboxSetupState.Error)
            {
                SetupState = ClipboxSetupState.Welcome;
            }

            DetectComplete?.Invoke(this, new EventArgs());
        }

        private void BootstrapperApplication_CacheAcquireProgress(Object SENDER, CacheAcquireProgressEventArgs e)
        {
            lock (_lockHandle)
            {
                _cacheProgress = e.OverallPercentage;
                Progress = (_cacheProgress + _executeProgress) / this.Phases;
                e.Result = _isCancelRequested ? Result.Cancel : Result.Ok;
            }
        }
        private void BootstrapperApplication_ApplyExecuteProgress(object sender, ExecuteProgressEventArgs e)
        {
            lock (_lockHandle)
            {

                _executeProgress = e.OverallPercentage;
                Progress = (_cacheProgress + _executeProgress) / 2; // always two phases if we hit execution.

                if (_model.BootstrapperApplication.Command.Display == Display.Embedded)
                {
                    _model.BootstrapperApplication.Engine.SendEmbeddedProgress(e.ProgressPercentage, this.Progress);
                }

                e.Result = _isCancelRequested ? Result.Cancel : Result.Ok;
            }
        }

        private void BootstrapperApplication_Progress(object sender, ProgressEventArgs e)
        {
            lock (_lockHandle)
            {
                e.Result = _isCancelRequested ? Result.Cancel : Result.Ok;
            }
        }

        private int _progress;
        public int Progress
        {
            get { 
                return SetupWorkflow == ClipboxSetupWorkflow.Modify 
                    && ModifyAction == ClipboxModifyAction.Uninstall
                    ? _progress * 2
                    : _progress; 
            }
            set { SetField(ref _progress, value, () => Progress); }
        }

        private bool _isRestartRequired;
        public bool IsRestartRequired
        {
            get { return _isRestartRequired; }
            set { SetField(ref _isRestartRequired, value, () => IsRestartRequired); }
        }

        private int _phases;
        public int Phases
        {
            get { return _phases; }
            set { SetField(ref _phases, value, () => Phases); }
        }

        private bool _isDetectionOngoing;
        public bool IsDetectionOngoing
        {
            get { return _isDetectionOngoing; }
            set { SetField(ref _isDetectionOngoing, value, () => IsDetectionOngoing); }
        }

        private bool _isNewerVersionAlreadyInstalled;
        public bool IsNewerVersionAlreadyInstalled
        {
            get { return _isNewerVersionAlreadyInstalled; }
            set { SetField(ref _isNewerVersionAlreadyInstalled, value, () => IsNewerVersionAlreadyInstalled); }
        }

        private ClipboxSetupState _setupState;
        public ClipboxSetupState SetupState
        {
            get { return _setupState; }
            set { SetField(ref _setupState, value, () => SetupState); }
        }

        private ClipboxModifyAction _modifyAction;
        public ClipboxModifyAction ModifyAction
        {
            get { return _modifyAction; }
            set { SetField(ref _modifyAction, value, () => ModifyAction); }
        }

        private ClipboxSetupWorkflow _setupWorkflow;
        public ClipboxSetupWorkflow SetupWorkflow
        {
            get { return _setupWorkflow; }
            set { SetField(ref _setupWorkflow, value, () => SetupWorkflow); }
        }

        private string _cultureCode;
        public string CultureCode
        {
            get { return _cultureCode; }
            set { SetField(ref _cultureCode, value, () => CultureCode); }
        }

        private string _installationDirectory;
        public string InstallationDirectory
        {
            get { return _installationDirectory; }
            set { SetField(ref _installationDirectory, value, () => InstallationDirectory); }
        }

        private bool _createDesktopShortcut;
        public bool CreateDesktopShortcut
        {
            get { return _createDesktopShortcut; }
            set { SetField(ref _createDesktopShortcut, value, () => CreateDesktopShortcut); }
        }

        private bool _pinTrayIcon = true;
        public bool PinTrayIcon
        {
            get { return _pinTrayIcon; }
            set { SetField(ref _pinTrayIcon, value, () => PinTrayIcon); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetField(ref _errorMessage, value, () => ErrorMessage); }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set { SetField(ref _message, value, () => Message); }
        }

        private void CloseInstallWindowCommandHandler()
        {
            if (SetupState == ClipboxSetupState.Done
                || SetupState == ClipboxSetupState.Error
                || SetupState == ClipboxSetupState.Welcome)
            {
                QuitInstallerInternal(errorCode: 0);
            }
            else
            {
                if (CancelCommand.CanExecute(null))
                {
                    CancelCommand.Execute(null);
                }
            }
        }

        private void QuitCommandHandler()
        {
            QuitInstallerInternal(errorCode: 0);
        }

        private void QuitInstallerInternal(int errorCode)
        {
            Task.Factory.StartNew(() => 
            {
                CancelEventArgs args = new CancelEventArgs();
                if ((SetupState == ClipboxSetupState.Welcome && SetupWorkflow != ClipboxSetupWorkflow.Modify && !_isNewerVersionAlreadyInstalled)
                    || SetupState == ClipboxSetupState.Preparing
                    || SetupState == ClipboxSetupState.Processing)
                {
                    QuitConfirmationRequested?.Invoke(this, args);
                }

                if (!args.Cancel)
                {
                    _model.QuitInstaller(errorCode);
                }
            });
        }

        public void QuitInstaller(int errorCode)
        {
            QuitInstallerInternal(errorCode);
        }

        public void LogMessage(string message)
        {
            _model.LogMessage(message);
        }
    }
}
