using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.DataObjects;
using Clipbox.Services.EventArgsClasses;
using System;

namespace Clipbox.Services.Interfaces
{
    public interface IPreviewGenerationService
    {
        bool TryGetQuickPreviewDimentionInfo(string clipboardItemId, MonitorDimensions monitorDimensions, out CalibratedPreviewDimentionInfo previewDimensionInfo);

        PreviewDataTransferObject GetPreviewDataTransferObject(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions);
    }
}
