using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions;
using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension]
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
    }
}
