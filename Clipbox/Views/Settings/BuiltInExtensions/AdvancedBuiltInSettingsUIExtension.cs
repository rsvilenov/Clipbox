using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class AdvancedBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        public override SettingsCategory Category => SettingsCategory.Advanced;

        protected override void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder)
        {
            builder
                .Add(SettingsKeys.AreFadeAnimationsEnabled, new FadeAnimationsSettingUIPart())
                .Add(SettingsKeys.IsBackgroundPreviewGenerationEnabled, new BackgroundPreviewGenerationSettingUIPart())
                .Add(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi, new SizeToDpiSettingUIPart());
        }
    }
}
