using Clipbox.Core.Configuration;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IConfigurationRecipient
    {
        /// <summary>
        /// This method is called by the system in order to
        /// set or update a setting in the extension. When
        /// <see cref="ISettingChangeRequestor.RequestSettingChange"/>
        /// is raised, this method gets called to update the setting
        /// in the extension after it has been saved by the system.
        /// </summary>
        /// <param name="settingKey">The key of the setting</param>
        /// <param name="settingValue">The value of the setting</param>
        void PutSetting(ISettingKey settingKey, object settingValue);
    }
}
