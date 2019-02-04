using Clipbox.Win32.Native;
using Setup.SetupActions.Interfaces;
using System;
using System.ComponentModel;

namespace Setup.SetupActions
{
    internal class DeskbandShellNotify : ISetupAction
    {
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        public void Do()
        {
            NotifyShell();
        }

        public void Undo()
        {
            NotifyShell();
        }

        private void NotifyShell()
        {
            Shell32.SHChangeNotify(NativeEnums.SHCNE.SHCNE_ASSOCCHANGED, NativeEnums.SHChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
