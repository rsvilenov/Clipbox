namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IHaveClipboardItemInfo
    {
        /// <summary>
        /// This is primarily meant to be used by the extentions
        /// </summary>
        object Tag { get; set; }

        string Id { get; }

        IClipboardItemSource Source { get; }

        long EntryDateTicks { get; }

        long EntryDateUtcTicks { get; }

        long ModifiedDateTicks { get; }

        long ModifiedDateUtcTicks { get; }
    }
}
