using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class PastingBuildInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        protected override SettingsCategory Category => SettingsCategory.Pasting;

        protected override void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder)
        {
            builder
                .Add(SettingsKeys.PutLastUsedQvItemsFirst, new PutLastUsedQvItemsFirstSettingUIPart());
        }
    }
}
