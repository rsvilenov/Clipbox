namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IHaveByteArray
    {
        /// <summary>
        /// The serialized data of the item
        /// </summary>
        byte[] ByteArray { get; }
    }
}
