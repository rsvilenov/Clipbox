using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class GeneralBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        public override SettingsCategory Category => SettingsCategory.General;

        protected override void OnSettingUpdated(ISettingKey settingKey)
        {
            if (settingKey == SettingsKeys.IsDeskBandEnabled)
            {
                // the tray icon and the deskband are mutually exclusive 
                RequestSettingChange(SettingsKeys.IsTrayIconEnabled, !GetSetting<bool>(SettingsKeys.IsDeskBandEnabled));
            }
        }

        protected override void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder)
        {
            builder
                .Add(SettingsKeys.IsClipboardTrackingEnabled, new ClipboardTrackingSettingUIPart())
                .Add(SettingsKeys.IsDeskBandEnabled, new PinTaskbarIconSettingUIPart());
        }
    }
}
