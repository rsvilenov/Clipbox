using Clipbox.Core.Configuration;
using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface ISettingsRegistrator
    {
        IEnumerable<ISettingKey> GetSettingKeys();
    }
}
