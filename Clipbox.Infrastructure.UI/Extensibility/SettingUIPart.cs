using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Infrastructure.UI.Extensibility
{
    public abstract class SettingUIPart<TSettingValue> : UserControl, ISettingUIPart
    {
        private event EventHandler _notifySettingModified;
        /// <summary>
        /// <see cref="ISettingUIPart.NotifySettingModified"/>
        /// </summary>
        event EventHandler ISettingUIPart.NotifySettingModified
        {
            add
            {
                _notifySettingModified -= value;
                _notifySettingModified += value;
            }

            remove
            {
                _notifySettingModified -= value;
            }
        }

        protected void NotifySettingModified()
        {
            _notifySettingModified?.Invoke(this, new EventArgs());
        }

        public abstract TSettingValue GetSettingValue();

        public abstract void SetSettingValue(TSettingValue value);

        void ISettingUIPart.SetSettingValue(object value)
        {
            SetSettingValue((TSettingValue)value);
        }

        object ISettingUIPart.GetSettingValue()
        {
            return GetSettingValue();
        }

        Type ISettingUIPart.GetSettingValueType()
        {
            return typeof(TSettingValue);
        }

        public void SetIsVisible(bool isVisible)
        {
            Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
