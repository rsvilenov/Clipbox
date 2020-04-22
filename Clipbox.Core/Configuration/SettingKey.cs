namespace Clipbox.Core.Configuration
{
    public class SettingKey<T>
    {
        public SettingKey(string name, T defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public string Name { get; }

        public T DefaultValue { get; }
    }
}
