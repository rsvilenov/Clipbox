using Clipbox.Core.Configuration;

namespace Clipbox.Infrastructure.Configuration
{
    public class SettingsProvider : IConfigurationProvider
    {
        public T GetSetting<T> (string settingKey)
        {
            return default(T);
        }

        public void SetSetting<T>(string settingKey, T value)
        {
        }
    }
}
