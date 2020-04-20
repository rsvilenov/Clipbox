using Clipbox.Core.Extensibility.EventArgsClasses;
using System;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IConfigurationChangeRequestor
    {
        /// <summary>
        /// Raises a request to change a setting.
        /// </summary>
        event EventHandler<RequestSettingChangeEventArgs> RequestSettingChange;
    }
}
