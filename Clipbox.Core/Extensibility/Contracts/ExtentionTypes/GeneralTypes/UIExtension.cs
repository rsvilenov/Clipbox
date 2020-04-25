using Clipbox.Core.ClipboardContent.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public abstract class UIExtension : ParametrizedExtension
    {
        private readonly Dictionary<string, string> _settings = new Dictionary<string, string>();

        /// <summary>
        /// Determines whether the extension should be shown
        /// </summary>
        /// <param name="selectedItems">A collection of the selected clipboard items</param>
        /// <returns></returns>
        public abstract bool OnShouldShow(IEnumerable<IClipboardItem> selectedItems);

        /// <summary>
        /// Allows creating a custom ui element, such as a button, for the view item of the current clipboard item.
        /// </summary>
        /// <param name="selectedItems"></param>
        /// <param name="additionalViewElement"></param>
        /// <param name="availableSize">The size available for fitting the additional view element</param>
        public virtual void OnExtensionViewGenerating(IEnumerable<IClipboardItem> selectedItems, ref UIElement additionalViewElement, Size availableSize)
        { }

        /// <summary>
        /// Allows adding some cleanup code when the clipboard view item is destroyed.
        /// </summary>
        public virtual void OnExtensionViewDestroyed()
        { }
        
        protected string GetSetting(string key)
        {
            return _settings.ContainsKey(key) ? _settings[key] : null;
        }

        protected void SetSetting(string key, string value)
        {
            if (_settings.ContainsKey(key))
            {
                _settings[key] = value;
            }
            else
            {
                _settings.Add(key, value);
            }
        }

        public void LoadSettings(IReadOnlyDictionary<string, string> settings)
        {
            _settings.Clear();
            foreach(var setting in settings)
            {
                _settings.Add(setting.Key, setting.Value);
            }
        }
        
        public IReadOnlyDictionary<string, string> GetSettings()
        {
            return new ReadOnlyDictionary<string, string>(_settings);
        }
    }
}
