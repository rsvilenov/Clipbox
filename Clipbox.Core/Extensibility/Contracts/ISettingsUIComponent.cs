using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Enums;
using System;
using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface ISettingsUIComponent
    {
        IReadOnlyDictionary<ISettingKey, ISettingUIPart> GetSettingsViewParts();

        void SetUpSettingUIParts();

        SettingsCategory GetCategory();

        IPreviewData GetResultPreviewData(PreviewParams previewParams);

        event EventHandler ResultPreviewInvalidated;
    }

}
