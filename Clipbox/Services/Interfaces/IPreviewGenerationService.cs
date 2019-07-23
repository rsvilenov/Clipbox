using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Services.DataObjects;
using Clipbox.ViewModels.BindableObjects;

namespace Clipbox.Services.Interfaces
{
    public interface IPreviewGenerationService
    {
        PreviewDimensionInfo GetQuickPreviewDimentionInfo(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions);

        ClipboardViewItem GetViewItem(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions);
    }
}
