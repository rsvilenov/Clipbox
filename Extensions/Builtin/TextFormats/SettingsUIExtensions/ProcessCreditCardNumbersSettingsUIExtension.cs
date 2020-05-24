using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions;
using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension]
    public class ProcessCreditCardNumbersSettingsUIExtension : SettingsUIExtension
    {
        protected override void RegisterSettings(IRegisteredSettingStore settingsStore)
        {
            settingsStore.Add<bool, ProcessCCNumbersFlowExtension>(ExtensionSettingNames.SkipMaskCreditCardNumbers, false);
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            builder.Add(ExtensionSettingNames.SkipMaskCreditCardNumbers, new ProcessCCNumbersSettingUIPart());
        }
    }
}
