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
using Clipbox.Views.ClipboardListView;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views
{
    public abstract class ClipboardViewControlBase : UserControl, IDisposable
    {
        public Dictionary<string, string> Parameters { get; private set; }

        public event EventHandler<CombinabilityEventArgs> CombinabilityCheckRequested;
        
        public event EventHandler<HousekeepingEventArgs> HousekeepingRequested;

        private UIExtentionContainer _container;

        private ISettingsService _settingsService;

        private readonly List<UIExtention> _loadedExtentions;
        
        private double _currentDisplayDpiCalibratedHeight;

        private Dictionary<string, string> _localExtentionSettings;

        protected MonitorDimensions MonitorDimensions { get; private set; }
        
        private Panel _uiExtentionContainer;

        private object _extensionLoadingLockHandle = new object();
        
        public double ClipboardViewItemMaxHeight
        {
            get { return (double)GetValue(ClipboardViewItemHeightProperty); }
            set { SetValue(ClipboardViewItemHeightProperty, value); }
        }
        
        public static readonly DependencyProperty ClipboardViewItemHeightProperty =
            DependencyProperty.Register(nameof(ClipboardViewItemMaxHeight), typeof(double), typeof(ClipboardViewControlBase), new PropertyMetadata(0d));

        
        /// <summary>
        /// Indicates whether we have ui extensions loaded. We cannot bind to uiExtensionContainer.Childern.Count because the
        /// collection does not raise change notifications. Therefore we need this property.
        /// </summary>
        public bool AreAnyUIExtensionsLoaded
        {
            get { return (bool)GetValue(AreAnyUIExtensionsLoadedperty); }
            set { SetValue(AreAnyUIExtensionsLoadedperty, value); }
        }
        
        public static readonly DependencyProperty AreAnyUIExtensionsLoadedperty =
            DependencyProperty.Register(nameof(AreAnyUIExtensionsLoaded), typeof(bool), typeof(ClipboardViewControlBase), new PropertyMetadata(false));


        protected double LoadingOfUIExtensionsDelayMs { get; set; } = 200d;

        /// <summary>
        /// This constructor is here solely because the XAML designer gives the following error otherwise:
        /// Error XLS0507 Type 'ClipboardViewControlBase' is not usable as an object element because it is not public or does not define a public parameterless constructor or a type converter."
        /// </summary>
        public ClipboardViewControlBase()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                throw new InvalidOperationException("This constructor should never be called from a user code.");
            }
        }

        public ClipboardViewControlBase(UIExtentionContainer container, ISettingsService settingsService)
        {
            _container = container;
            _settingsService = settingsService;
            _loadedExtentions = new List<UIExtention>();
            _localExtentionSettings = new Dictionary<string, string>();
            
            Loaded += ClipboardViewControlBase_Loaded;
        }

        private void ClipboardViewControlBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            _uiExtentionContainer = GetUIExtentionContainer();
            if (_uiExtentionContainer == null)
            {
                throw new NullReferenceException($"{nameof(GetUIExtentionContainer)} returned null.");
            }
        }
        
        
        protected void LoadUIExtensions(ClipboardListViewBase mainListView)
        {
            if (!mainListView.GetSelectedItemIds().Any()
                && !mainListView.GetSelectedMultiItemsIds().Any())
            {
                return;
            }

            // if ui extensions are or are being loaded, return
            if (_uiExtentionContainer.Children.Count > 0)
            {
                return;
            }
            
            lock (_extensionLoadingLockHandle)
            {
                // if the code inside the lock statement executed and set the 
                // AreAnyUIExtensionsLoaded property to true, there is no need
                // to execute it again after the lock has been released and there
                // are any waiting threads
                if (AreAnyUIExtensionsLoaded)
                {
                    return;
                }

                List<IClipboardItem> selectedClipboardItems = mainListView
                .SelectedItems
                .Cast<ClipboardViewItemWrapper>()
                .Select(w => w.ClipboardItem)
                .ToList();

                var allExtentions = GetExtentions(selectedClipboardItems);
                foreach (var extention in allExtentions)
                {
                    UIElement extentionUIElement = null;
                    extention.OnExtentionViewGenerating(selectedClipboardItems, ref extentionUIElement, new Size(_uiExtentionContainer.MaxWidth, _uiExtentionContainer.MaxHeight));
                    _uiExtentionContainer.Children.Add(extentionUIElement);
                }

                AreAnyUIExtensionsLoaded = allExtentions.Any();
            }
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

        public IReadOnlyList<IFlowParameter> GetFlowParameters()
        {
            var parameters = _loadedExtentions.SelectMany(ext => ext.GetAllParameters());
            var distinctParametersByKey = parameters.GroupBy(p => p.Key).Select(gr => gr.First());

            if (parameters.Count() != distinctParametersByKey.Count())
            {
                throw new InvalidOperationException("There are two UIExtention parameters with the same key");
            }
            
            return parameters.ToList().AsReadOnly();
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

        public virtual void ResumeDefaultState()
        { }

        public virtual void FocusNextItem()
        { }
        

        private void SetCurrentDisplayHeight(MonitorDimensions monitorDimensions)
        {
            bool resizeToDpi = _settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi);

            double currentDisplayHeight = monitorDimensions.WorkAreaHeight;
            if (resizeToDpi)
            {
                double scaleFactor = monitorDimensions.MonitorDPI / 96;
                currentDisplayHeight *= scaleFactor;
            }

            if (_currentDisplayDpiCalibratedHeight != currentDisplayHeight)
            {
                _currentDisplayDpiCalibratedHeight = currentDisplayHeight;
                // TODO: should we call OnClipboardViewItemHeightChanged when the ClipboardViewItemHeight setting changes?
                ClipboardViewItemMaxHeight = _currentDisplayDpiCalibratedHeight * _settingsService.GetSetting<double>(SettingsKeys.PreviewItemHeightRatio);
                OnClipboardViewItemMaxHeightChanged();
            }
        }

        public void SetMonitorDimensions(MonitorDimensions dimensions)
        {
            MonitorDimensions = dimensions;
            
            SetCurrentDisplayHeight(dimensions);
            OnSetControlMaxSizeRequested(dimensions);
        }

        protected virtual void OnSetControlMaxSizeRequested(MonitorDimensions monitorDimensions)
        { }
        
        protected virtual void OnClipboardViewItemMaxHeightChanged()
        { }
        

        protected void RaiseCombinabilityCheckRequested(CombinabilityEventArgs args)
        {
            CombinabilityCheckRequested?.Invoke(this, args);
        }

        

        protected void ClearLoadedUIExtensions()
        {
            _uiExtentionContainer.Children.Clear();
            AreAnyUIExtensionsLoaded = false;
        }

        protected void PerformHousekeeping(IEnumerable<string> clipboardItemIdsToProcess, out IReadOnlyCollection<string> removedClipboardItemIds)
        {
            HousekeepingEventArgs args = new HousekeepingEventArgs(clipboardItemIdsToProcess);
            HousekeepingRequested?.Invoke(this, args);
            removedClipboardItemIds = args.RemovedClipboardItemIds.ToList().AsReadOnly();
        }
        
        protected abstract Panel GetUIExtentionContainer();

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            
            // let us be safe in case the subclass forgets to call Dispose(bool)
            if (!_disposedValue)
            {
                _disposedValue = true;
            }
        }
        #endregion

        protected class SelectedItemsCombinabilityArgs
        {
            public SelectedItemsCombinabilityArgs(bool areItemsCombinable)
            {
                AreItemsCombinable = areItemsCombinable;
            }

            public bool AreItemsCombinable { get; }

            public bool CancelLastSelection { get; set; }
        }
    }
}
