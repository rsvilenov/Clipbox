namespace Clipbox.Core.Extensibility.Contracts
{
    public class PreviewParams
    {
        public PreviewParams(int previewWidth, int previewHeight, double displayDpi, bool areDimensionsScaledToDpi)
        {
            PreviewWidth = previewWidth;
            PreviewHeight = previewHeight;
            DisplayDpi = displayDpi;
            AreDimensionsScaledToDpi = areDimensionsScaledToDpi;
        }

        public int PreviewWidth { get; }

        public int PreviewHeight { get; }

        public double DisplayDpi { get; }

        public bool AreDimensionsScaledToDpi { get; }

    }
}
