using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class SecurityBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        protected override SettingsCategory Category => SettingsCategory.Security;

        protected override void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder)
        {
            builder.Add(SettingsKeys.AutoClearSysClipboardAfterSeconds, new AutoClearSysClipboardSettingUIPart())
                .Add(SettingsKeys.AutoClearExtClipboardAfterSeconds, new AutoClearExtClipboardSettingUIPart())
                .Add(SettingsKeys.IsAutoClearExtClipboardUponLockScreenEnabled, new AutoClearExtClipboardUponLockScreenSettingUIPart())
                .Add(SettingsKeys.IsAutoClearExtClipboardUponLogoffEnabled, new AutoClearExtClipboardUponLogoffSettingUIPart());
        }
    }
}
