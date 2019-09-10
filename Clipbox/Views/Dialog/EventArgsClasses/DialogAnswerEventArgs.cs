using Clipbox.Views.Dialog.Enums;
using System;

namespace Clipbox.Views.Dialog.EventArgsClasses
{
    public class DialogAnswerEventArgs : EventArgs
    {
        public DialogAnswerEventArgs(DialogButtons button)
        {
            SelectedButton = button;
        }

        public DialogButtons SelectedButton { get; }
    }
}
