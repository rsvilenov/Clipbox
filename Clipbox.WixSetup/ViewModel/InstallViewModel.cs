using Clipbox.WixSetup.Model;
using Setup.WixSetup.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Clipbox.WixSetup.ViewModel
{
    public class InstallViewModel : BindableObject
    {
        private readonly BootstrapperApplicationModel _model;

        public event EventHandler<CancelEventArgs> QuitConfirmationRequested;

        public ICommand QuitCommand { get; set; }
        public ICommand CloseInstallWindowCommand { get; set; }

        public InstallViewModel(BootstrapperApplicationModel model)
        {
            _model = model;

            QuitCommand = new DelegateCommand(QuitCommandHandler);
            CloseInstallWindowCommand = new DelegateCommand(CloseInstallWindowCommandHandler);
        }

        private void CloseInstallWindowCommandHandler()
        {
            QuitInstallerInternal(errorCode: 0);
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
                QuitConfirmationRequested?.Invoke(this, args);
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
