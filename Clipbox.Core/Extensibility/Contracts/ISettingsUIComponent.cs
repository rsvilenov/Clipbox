using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Enums;
using System;
using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface ISettingsUIComponent
    {
        IEnumerable<ISettingUIPart> GetSettingsViewParts();

        SettingsCategory Category { get; }

        IPreviewData GetResultPreview(PreviewParams previewParams);

        event EventHandler ResultPreviewInvalidated;
    }

}
