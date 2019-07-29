using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels.BindableObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views
{
    public class ClipboardViewControl : UserControl
    {
        public Dictionary<string, string> Parameters { get; private set; }
        
        public List<string> SelectedItemIds { get; protected set; }
        
        public Dictionary<string, List<int>> SelectedMultiItemsIds { get; protected set; }

        public event EventHandler<SelectionEventArgs> SelectionChanged;
        
        private UIExtentionContainer _container;

        private ISettingsService _settingsService;

        private readonly List<UIExtention> _loadedExtentions;
        
        private int _currentDisplayHeight;

        private Dictionary<string, string> _localExtentionSettings;

        protected MonitorDimensions MonitorDimensions { get; private set; }
        
        public int ClipboardViewItemMaxHeight
        {
            get { return (int)GetValue(ClipboardViewItemHeightProperty); }
            set { SetValue(ClipboardViewItemHeightProperty, value); }
        }
        
        public static readonly DependencyProperty ClipboardViewItemHeightProperty =
            DependencyProperty.Register(nameof(ClipboardViewItemMaxHeight), typeof(int), typeof(ClipboardViewControl), new PropertyMetadata(0));

        
        public ClipboardViewControl(UIExtentionContainer container, ISettingsService settingsService)
        {
            _container = container;
            _settingsService = settingsService;
            _loadedExtentions = new List<UIExtention>();
            _localExtentionSettings = new Dictionary<string, string>();
            Parameters = new Dictionary<string, string>();
            SelectedItemIds = new List<string>();
            SelectedMultiItemsIds = new Dictionary<string, List<int>>();
        }

        protected IEnumerable<UIExtention> GetExtentions(IEnumerable<IClipboardItem> clipboardItems)
        {
            _loadedExtentions.Clear();
            
            IEnumerable<UIExtention> extentionsToShow = _container.ResolveAll().Where(ext => CheckShouldLoadExtention(ext, clipboardItems));
            foreach (UIExtention extention in extentionsToShow)
            {
                _loadedExtentions.Add(extention);
                Dictionary<string, string> settingsForExtention = GetSettingsForExtention(extention);

                extention.LoadSettings(settingsForExtention);
            }

            return extentionsToShow;
        }

        private Dictionary<string, string> GetSettingsForExtention(UIExtention extention)
        {
            return _localExtentionSettings
                .Where(s => s.Key.StartsWith($"{extention.GetType().Name}:"))
                .ToDictionary(
                    // remove the prefix with the name of the extention
                    s => s.Key.Replace(extention.GetType().Name + ":", string.Empty),
                    s => _localExtentionSettings[s.Key]);
        }

        private bool CheckShouldLoadExtention(UIExtention extention, IEnumerable<IClipboardItem> clipboardItems)
        {
            try { return extention.OnShouldShow(clipboardItems); }
            catch (Exception ex)
            {
                throw new ClipboxExtentionException(nameof(extention.OnShouldShow), extention.GetType(), ex);
            }
        }

        public IEnumerable<IFlowParameter> GetParameters()
        {
            var parameters = _loadedExtentions.SelectMany(ext => ext.GetAllParameters());
            var distinctParametersByKey = parameters.GroupBy(p => p.Key).Select(gr => gr.First());

            if (parameters.Count() != distinctParametersByKey.Count())
            {
                throw new InvalidOperationException("There are two UIExtention parameters with the same key");
            }
            
            return parameters;
        }

        public void ClearParameters()
        {
            _loadedExtentions.ForEach(ext => ext.ClearParameters());
        }

        public void SaveChangedSettings()
        {
            foreach (UIExtention ext in _loadedExtentions)
            {
                IReadOnlyDictionary<string, string> settings = ext.GetSettings();
                foreach (string key in settings.Keys)
                {
                    /// TODO: this will not work, because the SettingsService requires the keys to 
                    /// be registered in the application configuration file first.
                    //string originalSetting = _settingsService.GetSetting<string>(key);
                    //if (originalSetting != settings[key])
                    //{
                    //    _settingsService.SetSetting(ext.GetType().Name + ":" + key, settings[key]);
                    //}
                    string keyWithExtentionType = ext.GetType().Name + ":" + key;
                    if (_localExtentionSettings.ContainsKey(keyWithExtentionType))
                    {
                        _localExtentionSettings[keyWithExtentionType] = settings[key];
                    }
                    else
                    {
                        _localExtentionSettings.Add(keyWithExtentionType, settings[key]);
                    }
                }
            }
        }

        public virtual void CancelSelectionWithMessage(ClipboardViewItem item, string message)
        { }

        public virtual void ResumeDefaultState()
        { }

        public virtual void FocusNextItem()
        { }


        protected SelectionChangedEventAnswer RaiseSelectionChangedEvent(IEnumerable<string> selectedItems)
        {
            var eventArgs = new SelectionEventArgs
            {
                SelectedItemIds = selectedItems,
                LastSelectedItemId = selectedItems.LastOrDefault()
            };

            SelectionChanged?.Invoke(this, eventArgs);

            return new SelectionChangedEventAnswer(eventArgs.Cancel, eventArgs.CancelMessage);
        }

        protected class SelectionChangedEventAnswer
        {
            public SelectionChangedEventAnswer(bool isCancelled, string cancelMessage = "")
            {
                IsCancelled = isCancelled;
                CancelMessage = cancelMessage;
            }

            public bool IsCancelled { get; }

            public string CancelMessage { get; }
        }

        public void SetCurrentDisplayHeight(int height)
        {
            if (_currentDisplayHeight != height)
            {
                _currentDisplayHeight = height;
                // TODO: should we call OnClipboardViewItemHeightChanged when the ClipboardViewItemHeight setting changes?
                ClipboardViewItemMaxHeight = (int)(_currentDisplayHeight * _settingsService.GetSetting<double>(SettingsKeys.PreviewItemHeightRatio));
                OnClipboardViewItemMaxHeightChanged();
            }
        }

        public void SetMonitorDimensions(MonitorDimensions dimensions)
        {
            MonitorDimensions = dimensions;
            SetCurrentDisplayHeight(dimensions.WorkAreaHeight);
        }
        
        public virtual void OnClipboardViewItemMaxHeightChanged()
        { }
    }
}
