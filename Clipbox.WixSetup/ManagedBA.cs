using Clipbox.WixSetup.Model;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Setup.WixSetup.View;
using Setup.WixSetup.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Clipbox.WixSetup
{
    public class ManagedBA : BootstrapperApplication
    {
        public ManagedBA()
        {
            // this is crucial! without it the resource dictionaries will not laod...
            if (Application.ResourceAssembly == null)
            {
                Application.ResourceAssembly = typeof(MainWindow).Assembly;
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


            var model = new BootstrapperApplicationModel(this);
            var viewModel = new InstallViewModel(model);
            InstallWindow View = new InstallWindow(viewModel);
            model.SetWindowHandle(View);
            this.Engine.Detect();
            View.Show();
            System.Windows.Threading.Dispatcher.Run();
            this.Engine.Quit(model.FinalResult);
        }
    }
}
