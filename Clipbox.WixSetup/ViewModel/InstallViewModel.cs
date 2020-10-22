using Clipbox.WixSetup.Model;
using Setup.WixSetup.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clipbox.WixSetup.ViewModel
{
    public class InstallViewModel : BindableObject
    {
        private readonly BootstrapperApplicationModel _model;

        public InstallViewModel(BootstrapperApplicationModel model)
        {
            _model = model;
        }

        public void QuitInstaller(int errorCode)
        {
            _model.QuitInstaller(errorCode);
        }
    }
}
