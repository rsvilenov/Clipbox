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
            // BootstrapperApplicationModel model = new BootstrapperApplicationModel(this);
            // InstallViewModel installVm = new InstallViewModel(model);
            // var mainWindow = new InstallWindow(installVm);
            //// mainWindow.Mba = this;
            //// mainWindow.InstallVm = installVm;
            // mainWindow.ShowDialog();
            // Engine.Quit(0);

            BootstrapperApplicationModel model = null;
            try
            {

                model = new BootstrapperApplicationModel(this);
                var viewModel = new InstallViewModel(model);
                InstallWindow View = new InstallWindow(viewModel);
                model.SetWindowHandle(View);
                this.Engine.Detect();
                if (this.Command.Display != Display.Embedded && this.Command.Display != Display.None)
                {
                    View.ShowDialog();
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
