using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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
            // make sure that all bindings in the ui part has already completed,
            // so that we do not run into some race condition with the binding updates.
            Dispatcher.BeginInvoke(new Action(() => 
                SetSettingValue((TSettingValue)value)), DispatcherPriority.ContextIdle);
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
