using Clipbox.WixSetup.Model;
using Clipbox.WixSetup.ViewModel;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Setup.WixSetup.View;
using System;
using System.Windows;


[assembly: BootstrapperApplication(typeof(Clipbox.WixSetup.ManagedBA))]

namespace Clipbox.WixSetup
{
    public class ManagedBA : BootstrapperApplication
    {
        public ManagedBA()
        {
            // this is crucial! without it the resource dictionaries will not laod...
            if (Application.ResourceAssembly == null)
            {
                Application.ResourceAssembly = GetType().Assembly;
            }
        }

        /// <summary>
        /// Entry point that is called when the bootstrapper application is ready to run.
        /// </summary>
        protected override void Run()
        {
            BootstrapperApplicationModel model = null;
            try
            {
                model = new BootstrapperApplicationModel(this);
                var viewModel = new InstallViewModel(model);
                InstallWindow View = new InstallWindow(viewModel);
                model.SetWindowHandle(View);
                this.Engine.Detect();

                bool isInQuietMode = this.Command.Display == Display.Embedded
                    || this.Command.Display == Display.None;
                if (!isInQuietMode)
                {
                    if (!(SystemParameters.PrimaryScreenWidth > 1280 &&
                       SystemParameters.PrimaryScreenHeight > 768)
                       && LaunchAction.Uninstall != this.Command.Action
                       && LaunchAction.Modify != this.Command.Action)
                    {
                        if (MessageBox.Show("Some features may not work properly on screen resolutions lower than 1280x760. Do you want to continue?"
                            , $"{ProductConstants.PRODUCT_NAME} Setup"
                            , MessageBoxButton.YesNo
                            , MessageBoxImage.Warning) == MessageBoxResult.No)
                        {
                            this.Engine.Quit(1);
                            return;
                        }
                    }

                    View.ShowDialog();
                }
                else if (LaunchAction.Uninstall != this.Command.Action)
                {
                    string quietModeError = "Only Uninstall is supported in quiet mode.";
                    Engine.Log(LogLevel.Error, quietModeError);
                    MessageBox.Show(quietModeError);
                    this.Engine.Quit(1);
                    return;
                }

                System.Windows.Threading.Dispatcher.Run();
                this.Engine.Quit(model.FinalResult);
            }
            catch(Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.ToString());
#endif
                this.Engine.Log(LogLevel.Error, ex.ToString());
                this.Engine.Quit(1);
            }
        }
    }
}
