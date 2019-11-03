namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface ITimestampInfo
    {
        long EntryDateTicks { get; }

        long EntryDateUtcTicks { get; }

        long ModifiedDateTicks { get; }

        long ModifiedDateUtcTicks { get; }
    }
}
