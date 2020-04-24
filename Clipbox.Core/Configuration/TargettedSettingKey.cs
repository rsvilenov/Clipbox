using System;

namespace Clipbox.Core.Configuration
{
    public class TargettedSettingKey<TSettingValue, TConsumerType> : 
        SettingKey<TSettingValue>, ITargettedSettingKey
    {
        public TargettedSettingKey(string name, TSettingValue defaultValue)
            : base(name, defaultValue)
        { }

        public Type ConsumerType => typeof(TConsumerType);
    }
}
