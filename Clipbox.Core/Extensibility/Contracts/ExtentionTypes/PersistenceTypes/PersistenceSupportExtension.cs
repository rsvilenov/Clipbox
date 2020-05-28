using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes
{
    public abstract class PersistenceSupportExtension<T> : IPersistenceSupport
        where T : IIntermediateFormat
    {

        /// <summary>
        /// Converts the byte array to a clipboard object, which can be taken by the relevant
        /// clipboard format extension and converted to a pointer, which can be passed to the clipboard.
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        public byte[] IntermediateFormatToByteArray(IIntermediateFormat intermediateFormat)
        {
            return GetByteArray((T)intermediateFormat);
        }

        protected abstract byte[] GetByteArray(T intermediateFormat);

        /// <summary>
        /// Converts the clipboard object data to a byte array. 
        /// The data may change during the process 
        /// (e.g. when getting a gdi+ handle from .NET's Metafile, the object gets invalidated).
        /// </summary>
        /// <param name="data">The data, as processed by the relevant clipboard format extension</param>
        /// <returns>Byte array of the data</returns>
        public IIntermediateFormat ByteArrayToIntermediateFormat(byte[] byteArray)
        {
            return GetIntermediateFormat(byteArray);
        }

        protected abstract T GetIntermediateFormat(byte[] byteArray);

        /// <summary>
        /// When set to true, indicates that the data
        /// should be encrypted when it is stored.
        /// </summary>
        public bool EncryptData { get; private set; }
        
        protected virtual bool ShouldEncryptData { get; }
    }
}
