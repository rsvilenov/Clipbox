namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public interface IPersistenceSupport : IClipboxExtention
    {
        byte[] IntermediateFormatToByteArray(IIntermediateFormat intermediateFormat);

        IIntermediateFormat ByteArrayToIntermediateFormat(byte[] byteArray);
    }
}
