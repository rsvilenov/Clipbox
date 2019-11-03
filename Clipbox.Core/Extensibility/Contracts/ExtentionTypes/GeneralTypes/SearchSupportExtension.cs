namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes
{
    public abstract class SearchSupportExtension<TIntermediateFormat> : ISearchSupport
        where TIntermediateFormat : IIntermediateFormat
    {
        protected abstract string GetSearchTextData(TIntermediateFormat intermediateFormat);

        public string GetSearchTextData(IIntermediateFormat intermediateFormat)
        {
            return GetSearchTextData((TIntermediateFormat)intermediateFormat);
        }
    }
}
