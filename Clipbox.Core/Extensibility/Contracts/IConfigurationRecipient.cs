using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.EventArgsClasses;
using System;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IConfigurationRecipient
    {
        /// <summary>
        /// This method is called by the system in order to
        /// set or update a setting in the extension. When
        /// <see cref="IConfigurationRecipient.RequestSettingChange"/>
        /// is raised, this method gets called to update the setting
        /// in the extension after it has been saved by the system.
        /// </summary>
        /// <param name="settingKey">The key of the setting</param>
        /// <param name="settingValue">The value of the setting</param>
        void PutSetting(ISettingKey settingKey, object settingValue);

        /// <summary>
        /// Raises a request to change a setting.  
        /// When the setting is changed, the system calls
        /// <see cref="IConfigurationRecipient.PutSetting(ISettingKey, object)"/>
        /// so that the extension updates its stored setting as well.
        /// </summary>
        event EventHandler<RequestSettingChangeEventArgs> RequestSettingChange;
    }
}
