using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
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

        private UIExtensionContainer _container;

        private ISettingsService _settingsService;

        private readonly List<UIExtension> _loadedExtensions;
        
        private double _currentDisplayDpiCalibratedHeight;

        private Dictionary<string, string> _localExtensionSettings;

        protected MonitorDimensions MonitorDimensions { get; private set; }
        
        private Panel _uiExtensionContainer;

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

        public ClipboardViewControlBase(UIExtensionContainer container, ISettingsService settingsService)
        {
            _container = container;
            _settingsService = settingsService;
            _loadedExtensions = new List<UIExtension>();
            _localExtensionSettings = new Dictionary<string, string>();
            
            Loaded += ClipboardViewControlBase_Loaded;
        }

        private void ClipboardViewControlBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            _uiExtensionContainer = GetUIExtensionContainer();
            if (_uiExtensionContainer == null)
            {
                throw new NullReferenceException($"{nameof(GetUIExtensionContainer)} returned null.");
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
            if (_uiExtensionContainer.Children.Count > 0)
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

                IEnumerable<ClipboardViewItemWrapper> multiItemItemWrappers = mainListView
                    .Items
                    .Cast<ClipboardViewItemWrapper>()
                    .Where(item => mainListView.GetSelectedMultiItemsIds().Keys.Contains(item.Id));

                selectedClipboardItems.AddRange(multiItemItemWrappers.Select(w => w.ClipboardItem));

                var allExtensions = GetExtensions(selectedClipboardItems);
                foreach (var extension in allExtensions)
                {
                    UIElement extensionUIElement = null;
                    extension.OnExtensionViewGenerating(selectedClipboardItems, ref extensionUIElement, new Size(_uiExtensionContainer.MaxWidth, _uiExtensionContainer.MaxHeight));
                    _uiExtensionContainer.Children.Add(extensionUIElement);
                }

                AreAnyUIExtensionsLoaded = allExtensions.Any();
            }
        }

        protected IEnumerable<UIExtension> GetExtensions(IEnumerable<IClipboardItem> clipboardItems)
        {
            _loadedExtensions.Clear();
            
            IEnumerable<UIExtension> extensionsToShow = _container.ResolveAll().Where(ext => CheckShouldLoadExtension(ext, clipboardItems));
            foreach (UIExtension extension in extensionsToShow)
            {
                _loadedExtensions.Add(extension);
                Dictionary<string, string> settingsForExtension = GetSettingsForExtension(extension);

                extension.LoadSettings(settingsForExtension);
            }

            return extensionsToShow;
        }

        private Dictionary<string, string> GetSettingsForExtension(UIExtension extension)
        {
            return _localExtensionSettings
                .Where(s => s.Key.StartsWith($"{extension.GetType().Name}:"))
                .ToDictionary(
                    // remove the prefix with the name of the extension
                    s => s.Key.Replace(extension.GetType().Name + ":", string.Empty),
                    s => _localExtensionSettings[s.Key]);
        }

        private bool CheckShouldLoadExtension(UIExtension extension, IEnumerable<IClipboardItem> clipboardItems)
        {
            try { return extension.OnShouldShow(clipboardItems); }
            catch (Exception ex)
            {
                throw new ClipboxExtensionException(nameof(extension.OnShouldShow), extension.GetType(), ex);
            }
        }

        public IReadOnlyList<IFlowParameter> GetFlowParameters()
        {
            var parameters = _loadedExtensions.SelectMany(ext => ext.GetAllParameters());
            var distinctParametersByKey = parameters.GroupBy(p => p.Key).Select(gr => gr.First());

            if (parameters.Count() != distinctParametersByKey.Count())
            {
                throw new InvalidOperationException("There are two UIExtension parameters with the same key");
            }
            
            return parameters.ToList().AsReadOnly();
        }

        public void ClearParameters()
        {
            _loadedExtensions.ForEach(ext => ext.ClearParameters());
        }

        public void SaveChangedSettings()
        {
            foreach (UIExtension ext in _loadedExtensions)
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
                    string keyWithExtensionType = ext.GetType().Name + ":" + key;
                    if (_localExtensionSettings.ContainsKey(keyWithExtensionType))
                    {
                        _localExtensionSettings[keyWithExtensionType] = settings[key];
                    }
                    else
                    {
                        _localExtensionSettings.Add(keyWithExtensionType, settings[key]);
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
            _uiExtensionContainer.Children.Clear();
            AreAnyUIExtensionsLoaded = false;
        }

        protected void PerformHousekeeping(IEnumerable<string> clipboardItemIdsToProcess, out IReadOnlyCollection<string> removedClipboardItemIds)
        {
            HousekeepingEventArgs args = new HousekeepingEventArgs(clipboardItemIdsToProcess);
            HousekeepingRequested?.Invoke(this, args);
            removedClipboardItemIds = args.RemovedClipboardItemIds.ToList().AsReadOnly();
        }
        
        protected abstract Panel GetUIExtensionContainer();

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
