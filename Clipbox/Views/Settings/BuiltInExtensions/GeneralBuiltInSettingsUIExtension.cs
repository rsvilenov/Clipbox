using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class GeneralBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        public override SettingsCategory Category => SettingsCategory.General;
       
        protected override void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder)
        {
            builder.Add(SettingsKeys.IsClipboardTrackingEnabled, new ClipboardTrackingSettingUIPart());
        }
    }
}
