using Clipbox.WixSetup.Model;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Setup.WixSetup.ViewModel.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Setup.WixSetup.ViewModel
{
    public class InstallViewModelOld : BindableObject
    {

        ///  <summary> 
        /// Record state
        ///  </summary> 
        private Enums.InstallState state;
        public UpdateState updatestate;

        ///  <summary> 
        /// need to be displayed in WPFWindow
        ///  </summary> 
        private string message;
        private BootstrapperApplicationModel model;
        private string _packageId = string.Empty;
        private bool canceled;
        private Dictionary<string, int> executingPackageOrderIndex;
        private string username;
        private int progress;
        private int cacheProgress;
        private int executeProgress;
        private Version _version = new Version(" 2.0.0.0 ");
        private bool _installEnabled;
        private int progressPhases = 1;
        private bool isUnstalling = false;
        #region Command
        ///  <summary> 
        /// Execute installation command
        // /  </summary> 
        public ICommand InstallCommand { get; private set; }
        public ICommand UninstallCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand LaunchNewsCommand { get; private set; }
        private ICommand repairCommand;

        public ICommand RepairCommand
        {
            get
            {
                return this.repairCommand ?? (this.repairCommand = new DelegateCommand(() =>
                model.PlanAction(LaunchAction.Repair)
                 , () => State == Enums.InstallState.Present));
            }
        }

        #endregion

        #region property
        public string Message
        {
            get { return message; }
            set { SetField(ref message, value); }
        }

        public Enums.InstallState State
        {
            get { return state; }
            set
            {
                if (state != value)
                {
                    SetField(ref state, value);

                    Message = $" Status: {state}";
                    Refresh();
                }
            }
        }


        public string PackageId
        {
            get { return _packageId; }
            set { SetField(ref _packageId, " packid: " + value); }
        }
        public bool Canceled
        {
            get { return canceled; }
            set { SetField(ref canceled, value); }
        }

        public Version Version
        {
            get { return _version; }
        }

        public string Username
        {
            get { return username; }
            set
            {
                SetField(ref username, value);
                this.model.SetBurnVariable("Username", this.username);
            }
        }



        public int Progress
        {
            get
            {
                if (isUnstalling)
                {
                    return progress * 2;
                }
                return progress;
            }
            set { SetField(ref progress, value); }
        }

        private string _info;

        public string Info
        {
            get
            {
                if (string.IsNullOrEmpty(_info))
                    _info = InstallEnabled ? " Installing... " : "In progress... ";
                return _info;
            }
            set { SetField(ref _info, value); }
        }

        private bool installEnabled;
        public bool InstallEnabled
        {
            get { return installEnabled; }
            set { SetField(ref installEnabled, value); }
        }

        private void UpdateInstallEnabled()
        {
            InstallEnabled = State == Enums.InstallState.NotPresent;
        }

        private bool uninstallEnabled;
        public bool UninstallEnabled
        {
            get { return uninstallEnabled; }
            set { SetField(ref uninstallEnabled, value); }
        }

        private void UpdateUninstallEnabled()
        {
            UninstallEnabled = UninstallCommand.CanExecute(this);
        }

        private bool cancenEnabled;
        public bool CancelEnabled
        {
            get { return cancenEnabled; }
            set { SetField(ref cancenEnabled, value); }
        }

        private void UpdateCancelEnabled()
        {
            CancelEnabled = State == Enums.InstallState.Applying;
        }

        private bool _exitEnabled;
        public bool ExitEnabled
        {
            get { return _exitEnabled; }
            set { SetField(ref _exitEnabled, value); }
        }

        public void UpdateExitEnabled()
        {
            ExitEnabled = this.State != Enums.InstallState.Applying;
        }

        private bool progressEnabled;
        public bool ProgressEnabled
        {
            get { return progressEnabled; }
            set { SetField(ref progressEnabled, value); }
        }

        private void UpdateProgressEnabled()
        {
            ProgressEnabled = this.State == Enums.InstallState.Applying;
        }

        ///  <summary> 
        ///Forget it for now
        ///  </summary> 
        public bool IsUpToDate
        {
            get { return true; }
        }
        public bool RepairEnabled
        {
            get { return this.RepairCommand.CanExecute(this); }
        }

        private bool _completeEnabled;
        public bool CompleteEnabled
        {
            get { return _completeEnabled; }
            set { SetField(ref _completeEnabled, value); }
        }

        private void UpdateCompleteEnabled()
        {
            CompleteEnabled = State == Enums.InstallState.Applied;
        }

        public int Phases
        {
            get { return progressPhases; }
            set { SetField(ref progressPhases, value); }
        }

        private string _installText = " Uninstall ";
        public string InstallText
        {
            get { return _installText; }
            set { SetField(ref _installText, value); }
        }

        public string RepairText
        {
            get { return _repairText; }
            set { SetField(ref _repairText, value); }
        }

        private bool _lableback = true;
        private string _repairText = " Repair ";

        public bool LabelBack
        {
            get { return _lableback; }
            set { _lableback = value; }
        }

        #endregion

        #region constructor
        ///  <summary> 
        /// constructor
        ///  </summary> 
        ///  <param name="_model"></param> 
        public InstallViewModelOld(BootstrapperApplicationModel _model)
        {
            model = _model;
            executingPackageOrderIndex = new Dictionary<string, int>();

            State = Enums.InstallState.Initializing;
            // Processing events triggered by bootstrapper 
            WireUpEventHandlers();
            //The first parameter of the initialization command is the method to be triggered by the command, and the second anonymous function is the condition for command execution. 
            InstallCommand = new DelegateCommand(() => model.PlanAction(LaunchAction.Install), () => State == Enums.InstallState.NotPresent);

            UninstallCommand = new DelegateCommand(() =>
            {
                model.PlanAction(LaunchAction.Uninstall);
                isUnstalling = true;
            }, () => State == Enums.InstallState.Present);

            CancelCommand = new DelegateCommand(() =>
            {
                model.LogMessage(" Cancelling... ");
                if (State == Enums.InstallState.Applying)
                {
                    State = Enums.InstallState.Cancelled;
                }
                else
                {
                    Application.Current.Dispatcher.InvokeShutdown();
                }
            }, () => State != Enums.InstallState.Cancelled);


            model.BootstrapperApplication.DetectComplete += DetectComplete;




            // Progress bar related event binding
            // this.model.BootstrapperApplication.CacheAcquireProgress +=
            // (sender, args) =>
            // {
            //     this.cacheProgress = args.OverallPercentage;
            //     this.Progress = (this. cacheProgress + this.executeProgress) / 2;
            // };
            // this.model.BootstrapperApplication.ExecuteProgress +=
            // (sender, args) =>
            // {
            //     this.executeProgress = args.OverallPercentage;
            //    this.Progress = (this.cacheProgress + this.executeProgress) / 2;
            // }; 
            model.BootstrapperApplication.CacheAcquireProgress += CacheAcquireProgress;
            model.BootstrapperApplication.ExecuteProgress += ApplyExecuteProgress;
            model.BootstrapperApplication.ExecuteMsiMessage += ExecuteMsiMessage;
            model.BootstrapperApplication.PlanBegin += PlanBegin;
            model.BootstrapperApplication.PlanPackageComplete += PlanPackageComplete;
            model.BootstrapperApplication.Progress += ApplyProgress;
            model.BootstrapperApplication.CacheComplete += CacheComplete;
        }

        #endregion

        private void DetectComplete(object sender, DetectCompleteEventArgs e)
        {
            if (LaunchAction.Uninstall == model.BootstrapperApplication.Command.Action)
            {
                model.BootstrapperApplication.Engine.Log(LogLevel.Verbose, " Invoking automatic plan for uninstall ");
                model.BootstrapperApplication.Engine.Plan(LaunchAction.Uninstall);
            }
            else if (e.Status == 0)
            {
                if (model.BootstrapperApplication.Engine.EvaluateCondition(" NETFRAMEWORK35_SP_LEVEL <1 "))
                {
                    string message = " WiX Toolset requires the .NET Framework 3.5.1 Windows feature to be enabled. ";
                    model.BootstrapperApplication.Engine.Log(LogLevel.Verbose, message);

                    if (Display.Full == model.BootstrapperApplication.Command.Display)
                    {
                        Application.Current.Dispatcher.Invoke((Action)delegate ()
                        {
                            MessageBox.Show(message, " DIAView ", MessageBoxButton.OK, MessageBoxImage.Error);
                            //if (null != CustomBootstrapperApplication.View)
                            //{
                            //    CustomBootstrapperApplication.View.Close();
                            //}
                        }
                        );
                    }

                    State = Enums.InstallState.Failed;
                    return;
                }
            }
            else
            {
                State = Enums.InstallState.Failed;
            }
        }

        #region method
        private void CacheAcquireProgress(Object SENDER, CacheAcquireProgressEventArgs e)
        {
            lock (this)
            {
                this.cacheProgress = e.OverallPercentage;
                this.Progress = (this.cacheProgress + this.executeProgress) / this.Phases;
                e.Result = Canceled ? Result.Cancel : Result.Ok;
            }
        }
        private void ApplyExecuteProgress(object sender, ExecuteProgressEventArgs e)
        {
            lock (this)
            {

                this.executeProgress = e.OverallPercentage;
                this.Progress = (this.cacheProgress + this.executeProgress) / 2; // always two phases if we hit execution.

                if (model.BootstrapperApplication.Command.Display == Display.Embedded)
                {
                    model.BootstrapperApplication.Engine.SendEmbeddedProgress(e.ProgressPercentage, this.Progress);
                }

                e.Result = Canceled ? Result.Cancel : Result.Ok;
            }
        }

        ///  <summary> 
        /// This method will be called in Detect
        ///  </summary> 
        ///  <param name="sender"></param> 
        ///  <param name="e"> </param> 
        protected void DetectPackageComplete(object sender, DetectPackageCompleteEventArgs e)
        {
            PackageId = e.PackageId;
            // corresponding to MsiPackage Id="DIAView" 
            if (e.PackageId.Equals("DIAView", StringComparison.Ordinal))
            {
                State = e.State == PackageState.Present ? Enums.InstallState.Present : Enums.InstallState.NotPresent;
            }
        }


        private void PlanBegin(object sender, PlanBeginEventArgs e)
        {
            lock (this)
            {
                if (InstallEnabled)
                {
                    Phases = (LaunchAction.Layout == model.PlannedAction) ? 1 : 2;
                }
                else
                {
                    LabelBack = false;
                }
                InstallText = "";
                RepairText = "";
                UpdateInstallEnabled();
                this.executingPackageOrderIndex.Clear();
            }
        }
        private void PlanPackageComplete(object sender, PlanPackageCompleteEventArgs e)
        {
            if (ActionState.None != e.Execute)
            {
                lock (this)
                {
                    Debug.Assert(!this.executingPackageOrderIndex.ContainsKey(e.PackageId));
                    this.executingPackageOrderIndex.Add(e.PackageId, this.executingPackageOrderIndex.Count);
                }
            }
        }

        ///  <summary> 
        /// This method will be triggered after PlanAction is over
        ///  </summary> 
        ///  <param name="sender"></param> 
        ///  <param name="e">< /param> 
        protected void PlanComplete(object sender, PlanCompleteEventArgs e)
        {
            if (State == Enums.InstallState.Cancelled)
            {
                Application.Current.Dispatcher.InvokeShutdown();
                return;
            }
            State = Enums.InstallState.Applying;
            model.ApplyAction();
        }
        ///  <summary> 
        /// ApplyAction starts 
        ///  </summary> 
        ///  <param name="sender"></param> 
        ///  <param name="e"></param> 
        protected void ApplyBegin(object sender, ApplyBeginEventArgs e)
        {
            State = Enums.InstallState.Applying;
            UpdateProgressEnabled();
            UpdateCancelEnabled();
        }
        ///  <summary> 
        /// installation
        ///  </summary> 
        ///  <param name="sender"></param> 
        ///  <param name="e"></param> 
        protected void ExecutePackageBegin(object sender, ExecutePackageBeginEventArgs e)
        {
            if (State == Enums.InstallState.Cancelled)
            {
                e.Result = Result.Cancel;
            }
        }

        private void ExecuteMsiMessage(object sender, ExecuteMsiMessageEventArgs e)
        {
            lock (this)
            {
                if (e.MessageType == Microsoft.Tools.WindowsInstallerXml.Bootstrapper.InstallMessage.ActionStart)
                {
                    this.Message = e.Message;
                }

                e.Result = Canceled ? Result.Cancel : Result.Ok;
            }
        }
        private void CacheComplete(object sender, CacheCompleteEventArgs e)
        {
            lock (this)
            {
                this.cacheProgress = 100;
                this.Progress = (this.cacheProgress + this.executeProgress) / this.progressPhases;
            }
        }
        ///  <summary> 
        /// Uninstall
        ///  </summary> 
        ///  <param name="sender"></param> 
        ///  <param name="e"></param> 
        protected void ExecutePackageComplete(object sender, ExecutePackageCompleteEventArgs e)
        {
            if (State == Enums.InstallState.Cancelled)
            {
                e.Result = Result.Cancel;
            }
        }
        ///  <summary> 
        /// End of Apply
        ///  </summary> 
        ///  <param name="sender"></param> 
        ///  <param name="e"></param> 
        protected void ApplyComplete(object sender, ApplyCompleteEventArgs e)
        {
            model.FinalResult = e.Status;
            State = Enums.InstallState.Applied;
            isUnstalling = false;
            UpdateCompleteEnabled();
            UpdateProgressEnabled();
            // CustomBootstrapperApplication.Dispatcher.InvokeShutdown(); 
        }

        private void ApplyProgress(object sender, ProgressEventArgs e)
        {
            lock (this)
            {
                e.Result = Canceled ? Result.Cancel : Result.Ok;
            }
        }
        ///  <summary> 
        /// Refresh the command status to change whether the UI is enabled
        ///  </summary> 
        private void Refresh()
        {
            Application.Current.Dispatcher.Invoke(
            (Action)(() =>
            {
                ((DelegateCommand)InstallCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)UninstallCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)CancelCommand).RaiseCanExecuteChanged();
            }));
        }
        ///  <summary> 
        /// Event Subscription
        ///  </summary> 
        private void WireUpEventHandlers()
        {
            model.BootstrapperApplication.DetectPackageComplete += DetectPackageComplete;

            model.BootstrapperApplication.PlanComplete += PlanComplete;

            model.BootstrapperApplication.ApplyComplete += ApplyComplete;

            model.BootstrapperApplication.ApplyBegin += ApplyBegin;

            model.BootstrapperApplication.ExecutePackageBegin += ExecutePackageBegin;

            model.BootstrapperApplication.ExecutePackageComplete += ExecutePackageComplete;

        }
        #endregion

        public void QuitInstaller()
        {
            model.QuitInstaller(0);
        }


    }

}