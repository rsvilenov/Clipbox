using Clipbox.Core.Configuration.Enums;
using System;

namespace Clipbox.Core.Configuration
{
    [Serializable]
    public class SettingObject
    {
        public SettingObject(string keyName, object value, SettingValueType type)
        {
            KeyName = keyName;
            Value = value;
            ValueType = type;
        }

        public string KeyName { get; }

        public SettingValueType ValueType { get; }

        public object Value { get; }
          
        public Type GetClrType(SettingValueType type)
        {
            switch (type)
            {
                case SettingValueType.String:
                    return typeof(string);
                case SettingValueType.Int:
                    return typeof(int);
                case SettingValueType.Double:
                    return typeof(double);
                default:
                    throw new InvalidOperationException($"Type not found. {nameof(SettingValueType)} does not correspond to the code in {nameof(GetClrType)}.");
            }
        }

        public SettingValueType GetSettingValueType(Type type)
        {
            if (type == typeof(int))
                return SettingValueType.String;
            if (type == typeof(int))
                return SettingValueType.Int;
            if (type == typeof(int))
                return SettingValueType.Double;

            throw new ArgumentException($"{type.Name} is not supported.");
        }
    }
}
