namespace Clipbox.ExtendedClipboard.EventArgs
{
    public class ClipboardManagerEventArgs : System.EventArgs
    {
        internal ClipboardManagerEventArgs(string itemId)
        {
            ItemId = itemId;
        }

        public string ItemId { get; private set; }
    }
}
