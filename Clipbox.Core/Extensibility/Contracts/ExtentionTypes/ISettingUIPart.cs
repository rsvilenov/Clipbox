using System;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public interface ISettingUIPart
    {
        void SetSettingValue(object value);

        object GetSettingValue();

        Type GetSettingValueType();

        event EventHandler NotifySettingModified;
    }
}
