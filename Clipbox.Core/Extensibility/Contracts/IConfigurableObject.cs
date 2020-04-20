using Clipbox.Core.Configuration;
using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IConfigurableObject
    {
        void LoadSettings(IEnumerable<SettingObject> settings);

        void UpdateSetting(SettingObject setting);
    }
}
