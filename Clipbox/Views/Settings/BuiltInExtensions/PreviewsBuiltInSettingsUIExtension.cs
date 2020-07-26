using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class PreviewsBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        public override SettingsCategory Category => SettingsCategory.Previews;

        protected override void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder)
        {
            builder
                .Add(SettingsKeys.MaxPreviewItemsInGroupItem, new MaxPreviewItemsInGroupSettingUIPart());
        }
    }
}
