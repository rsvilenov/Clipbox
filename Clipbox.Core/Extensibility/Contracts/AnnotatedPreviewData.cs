namespace Clipbox.Core.Extensibility.Contracts
{
    public abstract class AnnotatedPreviewData<TPreviewType> : BasePreviewData<TPreviewType>
    {
        /// <summary>
        /// A short description of the clipboard data (file name, dimentions, length, etc.)
        /// </summary>
        public string Annotation { get; set; }
    }
}
