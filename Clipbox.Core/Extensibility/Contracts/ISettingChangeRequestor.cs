using Clipbox.Core.Extensibility.EventArgsClasses;
using System;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface ISettingChangeRequestor
    {
        /// <summary>
        /// Raises a request to change a setting.  
        /// When the setting is changed, the system calls
        /// <see cref="IConfigurationRecipient.PutSetting(System.Core.Configuration.ISettingKey, object)"/>
        /// so that the extension updates its stored setting as well.
        /// </summary>
        event EventHandler<RequestSettingChangeEventArgs> RequestSettingChange;
    }
}
