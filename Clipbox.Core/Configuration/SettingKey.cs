using System;

namespace Clipbox.Core.Configuration
{
    /// <summary>
    /// A key object for accessing a setting
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SettingKey<T> : ISettingKey
    {
        /// <summary>
        /// Creates a SettingKey object
        /// </summary>
        /// <param name="name">The name of the key</param>
        /// <param name="defaultValue">The type of the value of the setting</param>
        public SettingKey(string name, T defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// The name of the key
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The default value
        /// </summary>
        public T DefaultValue { get; }

        
        /// <summary>
        /// <see cref="ISettingKey.GetTypeOfValue"/>
        /// </summary>
        Type ISettingKey.TypeOfValue => typeof(T);

        /// <summary>
        /// <see cref="ISettingKey.GetDefaultValue"/>
        /// </summary>
        /// <returns></returns>
        object ISettingKey.DefaultValue => DefaultValue;

        /// <summary>
        /// The name of the key
        /// <see cref="ISettingKey.Name"/>
        /// </summary>
        string ISettingKey.Name => Name;

        public override bool Equals(object obj)
        {
            return GetHashCode() == ((ISettingKey)obj).GetHashCode();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Name.GetHashCode();
                hash = hash * 23 + typeof(T).GetHashCode();
                return hash;
            }
        }

    }
}
