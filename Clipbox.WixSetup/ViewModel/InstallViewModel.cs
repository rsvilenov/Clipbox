using Clipbox.WixSetup.Model;
using Clipbox.WixSetup.ViewModel.Enums;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Setup.WixSetup.ViewModel;
using System;
using System.ComponentModel;
using System.Diagnostics;
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

        public ICommand QuitCommand { get; set; }
        public ICommand CloseInstallWindowCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand InstallCommand { get; set; }
        public ICommand UninstallCommand { get; set; }
        public ICommand UpdateCommand { get; set; }
        public ICommand RepairCommand { get; set; }
        public ICommand ModifyCommand { get; set; }
        public ICommand RetryCommand { get; set; }

        private bool _isCancelRequested;
        private bool _isSetupActionRequested;
        private int _cacheProgress;
        private int _executeProgress;
        private LaunchAction _lastLaunchAction;

        public InstallViewModel(BootstrapperApplicationModel model)
        {
            _model = model;
            InstallationDirectory =
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), ProductConstants.COMPANY_NAME, ProductConstants.PRODUCT_NAME);

            QuitCommand = new DelegateCommand(QuitCommandHandler);
            CloseInstallWindowCommand = new DelegateCommand(CloseInstallWindowCommandHandler);
            CancelCommand = new DelegateCommand(CancelCommandHandler, () => !_isCancelRequested);
            InstallCommand = new DelegateCommand(() => RequestSetupAction(LaunchAction.Install), () => !_isSetupActionRequested);
            UninstallCommand = new DelegateCommand(() => RequestSetupAction(LaunchAction.Uninstall), () => !_isSetupActionRequested);
            RepairCommand = new DelegateCommand(() => RequestSetupAction(LaunchAction.Repair), () => !_isSetupActionRequested);
            UpdateCommand = new DelegateCommand(UpdateCommandHandler, () => !_isSetupActionRequested);
            ModifyCommand = new DelegateCommand(() => throw new NotImplementedException(nameof(ModifyCommand)));
            RetryCommand = new DelegateCommand(RetryCommandHandler, () => !_isSetupActionRequested);

            _model.BootstrapperApplication.DetectBegin += BootstrapperApplication_DetectBegin;
            _model.BootstrapperApplication.DetectComplete += BootstrapperApplication_DetectComplete;
            _model.BootstrapperApplication.DetectPackageComplete += BootstrapperApplication_DetectPackageComplete;
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

        private void CancelCommandHandler()
        {
            CancelEventArgs args = new CancelEventArgs();
            CancelConfirmationRequested?.Invoke(this, args);
            if (!args.Cancel)
            {
                _isCancelRequested = true;
            }
        }

        private void RetryCommandHandler()
        {
            RequestSetupAction(_lastLaunchAction);
        }

        private void RequestSetupAction(LaunchAction launchAction)
        {
            _isSetupActionRequested = true;

            _lastLaunchAction = launchAction;

            switch (launchAction)
            {
                case LaunchAction.Repair:
                    ModifyAction = ClipboxModifyAction.Repair;
                    break;
                case LaunchAction.Uninstall:
                    ModifyAction = ClipboxModifyAction.Modify;
                    break;
                case LaunchAction.Modify:
                    ModifyAction = ClipboxModifyAction.Modify;
                    break;
            }

            _model.PlanAction(launchAction);
        }

        private void UpdateCommandHandler()
        {
            Process.Start(ProductConstants.PRODUCT_UPDATE_URL);
            QuitInstallerInternal(0);
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
            SetupState = ClipboxSetupState.Done;
        }

        private void BootstrapperApplication_ApplyBegin(object sender, ApplyBeginEventArgs e)
        {
            SetupState = ClipboxSetupState.Processing;
        }

        private void BootstrapperApplication_PlanBegin(object sender, PlanBeginEventArgs e)
        {
            Phases = (LaunchAction.Layout == _model.PlannedAction
                || LaunchAction.Uninstall == _model.PlannedAction) ? 1 : 2;
        }

        private void BootstrapperApplication_PlanComplete(object sender, PlanCompleteEventArgs e)
        {
            if (_isCancelRequested)
            {
                QuitInstallerInternal(0);
                return;
            }

            SetupState = ClipboxSetupState.Processing;

            _model.ApplyAction();
        }

        private void BootstrapperApplication_DetectPackageComplete(object sender, Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectPackageCompleteEventArgs e)
        {
            if (e.PackageId.Equals(ProductConstants.MSI_PACKAGE_ID, StringComparison.Ordinal))
            {
                SetupWorkflow = e.State == PackageState.Present 
                    ? ClipboxSetupWorkflow.Modify : ClipboxSetupWorkflow.Install;
            }
        }

        private void BootstrapperApplication_DetectBegin(object sender, Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectBeginEventArgs e)
        {
            IsDetectionOngoing = true;
        }

        private void BootstrapperApplication_DetectComplete(object sender, Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectCompleteEventArgs e)
        {
            IsDetectionOngoing = false;
            switch (_model.BootstrapperApplication.Command.Action)
            {
                case LaunchAction.Uninstall:
                    SetupWorkflow = ClipboxSetupWorkflow.Modify;
                    break;
                case LaunchAction.UpdateReplace:
                    SetupWorkflow = ClipboxSetupWorkflow.Update;
                    break;
                case LaunchAction.Install:
                    SetupWorkflow = ClipboxSetupWorkflow.Install;
                    break;
            }
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
                Progress = (_cacheProgress + _executeProgress) / (ModifyAction == ClipboxModifyAction.Uninstall ? 1 : 2); // always two phases if we hit execution.

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
            get { return _progress; }
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

        private string _languageCode;
        public string LanguageCode
        {
            get { return _languageCode; }
            set { SetField(ref _languageCode, value, () => LanguageCode); }
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
                if (SetupState == ClipboxSetupState.Welcome
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
    }
}
