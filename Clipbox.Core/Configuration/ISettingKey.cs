using System;

namespace Clipbox.Core.Configuration
{
    /// <summary>
    /// Describes a key object for a setting
    /// </summary>
    public interface ISettingKey
    {
        /// <summary>
        /// The key name of the setting
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the type of the value for this setting.
        /// </summary>
        Type TypeOfValue { get; }

        /// <summary>
        /// The default value of the setting.
        /// </summary>
        object DefaultValue { get; }
    }
}
