using Clipbox.ExtendedClipboard.Enums;

namespace Clipbox.ExtendedClipboard.EventArgs
{
    public class ItemChangedEventArgs : ClipboardManagerEventArgs
    {
        public ItemChangedEventArgs(string itemId, TypeOfItemChange typeOfChange)
            : base (itemId)
        {
            TypeOfChange = typeOfChange;
        }

        public TypeOfItemChange TypeOfChange { get; }
    }
}
