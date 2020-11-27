using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions;
using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension(
        description: "Setting UI extension for the extensions which process the copied text before storing",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
    )]
    public class TextPreProcessingSettingUIExtension : SettingsUIExtension
    {
        protected override void RegisterSettings(IRegisteredSettingStore settingsStore)
        {
            settingsStore.Add<int, SkipShortStringsFlowExtension>(ExtensionSettingNames.MinTextLengthToStore, 0 /* 0 means infinity */);
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            builder.Add(ExtensionSettingNames.MinTextLengthToStore, new SkipShortStringsSettingsUIPart());
        }

        protected override SettingsCategory Category => SettingsCategory.Copying;
    }
}
