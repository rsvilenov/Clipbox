namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IClipboardDataObject : IHaveClipboardFormat
    {
        /// <summary>
        /// The original clipboard object, acquired after processing by the appropriate clipboard format extention
        /// </summary>
        object OriginalData { get; }
    }
}
