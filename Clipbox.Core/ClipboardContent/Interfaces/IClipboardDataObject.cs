namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IClipboardDataObject : IHaveClipboardFormat
    {
        /// <summary>
        /// The original clipboard object, aquired after processing by the appropriate clipboard format extention
        /// </summary>
        object OriginalData { get; }
    }
}
