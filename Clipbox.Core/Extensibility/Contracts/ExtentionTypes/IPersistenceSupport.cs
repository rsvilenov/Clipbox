namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public interface IPersistenceSupport : IClipboxExtension
    {
        byte[] IntermediateFormatToByteArray(IIntermediateFormat intermediateFormat);

        IIntermediateFormat ByteArrayToIntermediateFormat(byte[] byteArray);

        bool EncryptData { get; }
    }
}
