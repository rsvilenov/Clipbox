namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public interface ISearchSupport : IClipboxExtention
    {
        string GetSearchTextData(IIntermediateFormat intermediateFormat);
    }
}
