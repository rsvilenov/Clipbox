namespace Clipbox.Core.Extensibility.Contracts
{
    public abstract class BasePreviewData<TPreviewType> : IPreviewData
    {
        public TPreviewType Preview { get; set; }
    }
}
