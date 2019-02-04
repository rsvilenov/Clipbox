using System;
using System.ComponentModel;

namespace Setup.SetupActions.Interfaces
{
    internal interface ISetupAction
    {
        void Do();

        void Undo();

        event EventHandler<ProgressChangedEventArgs> ProgressChanged;
    }
}
