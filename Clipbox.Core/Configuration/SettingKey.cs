using System;
using System.Diagnostics;

namespace Clipbox.Core.Configuration
{
    /// <summary>
    /// A key object for accessing a setting with a target component specified.
    /// </summary>
    /// <typeparam name="TSettingValueType">The type of the setting value</typeparam>
    /// <typeparam name="TTargetComponentType">The type of the component, which will use the setting</typeparam>
    [DebuggerDisplay("{Name} (default: {DefaultValue}) [target: {TargetComponentType.Name}]")]
    public class SettingKey<TSettingValueType, TTargetComponentType> : SettingKey<TSettingValueType>
    {
        public SettingKey(string name, TSettingValueType defaultValue)
            : base(name, defaultValue)
        {
            SetTargetComponentType(typeof(TTargetComponentType));
        }
    }

    /// <summary>
    /// A key object for accessing a setting
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    [DebuggerDisplay("{Name} (default: {DefaultValue})")]
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

        private Type _targetComponentType;
        /// <summary>
        /// The type of the component which will use this setting (optional).
        /// </summary>
        Type ISettingKey.TargetComponentType { get { return _targetComponentType; } }

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

        protected void SetTargetComponentType(Type type)
        {
            _targetComponentType = type;
        }

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

                if (_targetComponentType != null)
                {
                    hash = hash * 23 + _targetComponentType.GetHashCode();
                }

                return hash;
            }
        }

    }
}
