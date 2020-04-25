namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IHaveClipboardItemInfo
    {
        /// <summary>
        /// This is primarily meant to be used by the extensions
        /// </summary>
        object Tag { get; set; }

        string Id { get; }

        IClipboardItemSource Source { get; }

        ITimestampInfo TimestampInfo { get; }
    }
}
