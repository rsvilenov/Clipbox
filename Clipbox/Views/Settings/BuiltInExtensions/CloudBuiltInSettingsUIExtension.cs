using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class CloudBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        public override SettingsCategory Category => SettingsCategory.Cloud;

        protected override void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder)
        {
            builder
                .Add(SettingsKeys.IsCloudSyncEnabled, new IsCloudEnabledSettingUIPart());
        }

    }
}
