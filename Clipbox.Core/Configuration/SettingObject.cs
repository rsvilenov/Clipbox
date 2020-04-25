using Clipbox.Core.Configuration.Enums;
using System;
using System.Diagnostics;

namespace Clipbox.Core.Configuration
{
    [Serializable]
    [DebuggerDisplay("{Value} (type: {ValueType})")]
    public class SettingObject
    {
        public SettingObject(string keyId, object value, SettingValueType type)
        {
            KeyId = keyId;
            Value = value;
            ValueType = type;
        }

        public string KeyId { get; }

        public SettingValueType ValueType { get; }

        public object Value { get; }
          
        public static Type GetClrType(SettingValueType type)
        {
            switch (type)
            {
                case SettingValueType.String:
                    return typeof(string);
                case SettingValueType.Int:
                    return typeof(int);
                case SettingValueType.Double:
                    return typeof(double);
                case SettingValueType.Boolean:
                    return typeof(bool);
                default:
                    throw new InvalidOperationException($"Type not found. {nameof(SettingValueType)} does not correspond to the code in {nameof(GetClrType)}.");
            }
        }

        public static SettingValueType GetSettingValueType(Type type)
        {
            if (type == typeof(string))
                return SettingValueType.String;
            if (type == typeof(int))
                return SettingValueType.Int;
            if (type == typeof(double))
                return SettingValueType.Double;
            if (type == typeof(bool))
                return SettingValueType.Boolean;

            throw new ArgumentException($"{type.Name} is not supported.");
        }
    }
}
