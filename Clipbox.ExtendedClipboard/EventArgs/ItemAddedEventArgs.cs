using Clipbox.ExtendedClipboard.Enums;

namespace Clipbox.ExtendedClipboard.EventArgs
{
    public class ItemAddedEventArgs : ClipboardManagerEventArgs
    {
        public ItemAddedEventArgs(string itemId, ItemSource source)
            : base(itemId)
        {
            Source = source;
        }

        public ItemSource Source { get; }
    }
}
