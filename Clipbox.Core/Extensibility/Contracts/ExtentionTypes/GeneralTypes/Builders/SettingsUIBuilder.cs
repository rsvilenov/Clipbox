using Clipbox.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes.Builders
{
    public class SettingsUIBuilder : ISettingsUIBuilder
    {
        private readonly Dictionary<ISettingKey, ISettingUIPart> _parts;
        private readonly IDictionary<ISettingKey, object> _settings;

        public SettingsUIBuilder(IDictionary<ISettingKey, object> settings)
        {
            _parts = new Dictionary<ISettingKey, ISettingUIPart>();
            _settings = settings;
        }

        public ISettingsUIBuilder Add(string settingName, ISettingUIPart uiPart)
        {
            if (!(uiPart is ISettingUIPart))
            {
                throw new ArgumentException($"{nameof(uiPart)} should implement {nameof(ISettingUIPart)}.");
            }

            foreach (ISettingKey settingKey in _settings.Keys
                .Where(k => k.Name == settingName
                    && uiPart.GetSettingValueType() == k.TypeOfValue))
            {

                if (settingKey == null)
                {
                    throw new ArgumentException($"{nameof(settingName)} : {settingName} of type {uiPart.GetSettingValueType().Name} is not registered.");
                }

                _parts.Add(settingKey, uiPart);
            }

            return this;
        }

        public Dictionary<ISettingKey, ISettingUIPart> GetSettingKeyUIPartMapping()
        {
            return _parts;
        }
    }
}
