using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class TimestampInfo : ITimestampInfo
    {
        public TimestampInfo(long entryDateTicks,
            long entryDateUtcTicks,
            long modifiedDateTicks,
            long modifiedDateUtcTicks)
        {
            EntryDateTicks = entryDateTicks;
            EntryDateUtcTicks = entryDateUtcTicks;
            ModifiedDateTicks = modifiedDateTicks;
            ModifiedDateUtcTicks = modifiedDateUtcTicks;
        }

        public TimestampInfo(ITimestampInfo tsInfo)
        {
            EntryDateTicks = tsInfo.EntryDateTicks;
            EntryDateUtcTicks = tsInfo.EntryDateUtcTicks;
            ModifiedDateTicks = tsInfo.ModifiedDateTicks;
            ModifiedDateUtcTicks = tsInfo.ModifiedDateUtcTicks;
        }

        public TimestampInfo() { }

        public long EntryDateTicks { get; set; }

        public long EntryDateUtcTicks { get; set; }

        public long ModifiedDateTicks { get; set; }

        public long ModifiedDateUtcTicks { get; set; }
    }
}
