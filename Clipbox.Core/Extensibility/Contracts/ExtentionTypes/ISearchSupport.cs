namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public interface ISearchSupport : IClipboxExtension
    {
        string GetSearchTextData(IIntermediateFormat intermediateFormat);
    }
}
