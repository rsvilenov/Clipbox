using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI.Extensibility
{
    public abstract class SettingUIPart<TSettingValue> : UserControl, ISettingUIPart, IDisposable
    {
        private const int SETTING_CHANGE_NOTIFICATION_THROTTLE_MS = 500;
        private SlidingTimer _notifySettingsModifiedSlidingTImer;
        private bool _isDisposed;

        public SettingUIPart()
        {
            _notifySettingsModifiedSlidingTImer = new SlidingTimer
            {
                IntervalMs = SETTING_CHANGE_NOTIFICATION_THROTTLE_MS
            };
            _notifySettingsModifiedSlidingTImer.Elapsed += _notifySettingsModifiedSlidingTImer_Elapsed;
        }

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
            _notifySettingsModifiedSlidingTImer.Slide();
        }

        private void _notifySettingsModifiedSlidingTImer_Elapsed(object sender, EventArgs e)
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

        protected virtual void Dispose(bool disposing)
        {
            _notifySettingsModifiedSlidingTImer.Dispose();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Dispose(disposing: true);
                _isDisposed = true;
            }
        }
    }
}
