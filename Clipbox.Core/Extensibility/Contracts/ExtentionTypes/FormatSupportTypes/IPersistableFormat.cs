namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    /// <summary>
    /// Allows binary serialization of the clipbard data, as adapted by a ClpboardFormatSupport extention
    /// </summary>
    public interface IPersistableFormat : IClipboxExtention // implements IClipboxtention in order to allow it to be used by the ExtentionInvoker
    {
        /// <summary>
        /// Converts the clipboard object data to a byte array. 
        /// The data may change during the process 
        /// (e.g. when getting a gdi+ handle from .NET's Metafile, the object gets invalidated).
        /// </summary>
        /// <param name="data">The data, as processed by the relevant clipboard format extention</param>
        /// <returns>Byte array of the data</returns>
        byte[] ObjectToByteArray(ref object data);

        /// <summary>
        /// Converts the byte array to a clipboard object, which can be taken by the relevant
        /// clipboard format extention and converted to a pointer, which can be passed to the clipboard.
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        object ByteArrayToObject(byte[] byteArray);

        /// <summary>
        /// Specified whether the data has meaning only on the machine it is collected on. 
        /// Filesystem paths (CF_HDROP) for example are hardly the same on another machine. 
        /// </summary>
        bool RequiresSameMachine { get; }
    }
}
