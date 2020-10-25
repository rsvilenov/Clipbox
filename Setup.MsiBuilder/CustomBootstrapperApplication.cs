using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Setup.MsiBuilder.Model;
using Setup.MsiBuilder.View;
using Setup.MsiBuilder.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

[assembly: BootstrapperApplication(typeof(Setup.MsiBuilder.CustomBootstrapperApplication))]

namespace Setup.MsiBuilder
{
    public class CustomBootstrapperApplication : BootstrapperApplication
    {
        //public static Dispatcher Dispatcher { get; set; }
        //static public InstallWindow View { get; private set; }

        protected override void Run()
        {
            MessageBox.Show("Starting setup");
            Engine.Quit(0);
            //var model = new BootstrapperApplicationModel(this);
            //var viewModel = new InstallViewModel(model);
            //InstallWindow View = new InstallWindow(viewModel);
            //model.SetWindowHandle(View);
            //this.Engine.Detect();
            //View.Show();
            //Dispatcher.Run();
            //this.Engine.Quit(model.FinalResult);
        }
    }
}
