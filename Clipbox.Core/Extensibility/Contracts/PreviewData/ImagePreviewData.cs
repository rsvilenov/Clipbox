using System;
using System.Drawing;

namespace Clipbox.Core.Extensibility.Contracts.PreviewData
{
    // TODO: This should use ImageSource, not Image
    public class ImagePreviewData : AnnotatedPreviewData<Image>, IDisposable
    {
        public void Dispose()
        {
            if (Preview != null)
            {
                Preview.Dispose();
            }
        }
    }
}
