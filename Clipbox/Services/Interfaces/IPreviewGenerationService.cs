using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Services.DataObjects;
using Clipbox.ViewModels.BindableObjects;

namespace Clipbox.Services.Interfaces
{
    public interface IPreviewGenerationService
    {
        bool TryGetQuickPreviewDimentionInfo(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions, out PreviewDimensionInfo previewDimensionInfo);

        ClipboardViewItem GetViewItem(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions);
    }
}
