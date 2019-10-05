namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes
{
    public abstract class SearchSupportExtension : IClipboxExtention
    {
        public abstract string GetSearchTextData(object data);
    }
}
