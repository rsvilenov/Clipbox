using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.EventArgsClasses;
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

        private readonly List<IFlowUIExtension> _loadedExtensions;

        private readonly object _loadedExtensionsLockHandle = new object();

        private double _currentDisplayDpiCalibratedHeight;
        
        protected MonitorDimensions MonitorDimensions { get; private set; }
        
        private Panel _uiExtensionContainer;

        private readonly object _extensionLoadingLockHandle = new object();
        
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
            _loadedExtensions = new List<IFlowUIExtension>();
            
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
                lock (_extensionLoadingLockHandle)
                {
                    if (_loadedExtensions.Any())
                    {
                        ShutDownExtensions(_loadedExtensions);
                    }
                }

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

                if (_loadedExtensions.Any())
                {
                    ShutDownExtensions(_loadedExtensions);
                }

                if (_loadedExtensions.Any())
                {
                    throw new InvalidOperationException("There should not be loaded extensions at this stage.");
                }

                var extensions = GetExtensions(selectedClipboardItems);

                SetUpExtensions(extensions);

                foreach (var extension in extensions)
                {
                    IFlowExtensionUIControl extensionUIControl = null;

                    try { extensionUIControl = extension.CreateExtensionUI(selectedClipboardItems, _uiExtensionContainer.MaxWidth, _uiExtensionContainer.MaxHeight); }
                    catch (Exception ex) { throw new ClipboxExtensionException(nameof(extension.CreateExtensionUI), extension.GetType(), ex); }

                    UIElement extensionUIElement = extensionUIControl as UIElement;
                    if (extensionUIElement == null)
                    {
#if DEBUG
                        throw new InvalidCastException($"{extension.GetType().Name} did not create an element of type UIElement when {nameof(extension.CreateExtensionUI)} was called");
#endif
                    }

                    _uiExtensionContainer.Children.Add(extensionUIElement);
                }

                AreAnyUIExtensionsLoaded = _loadedExtensions.Any();
            }
        }
        
        private IEnumerable<IFlowUIExtension> GetExtensions(IEnumerable<IClipboardItem> clipboardItems)
        {
            return _container
                .ResolveAll()
                .Where(ext => CheckShouldLoadExtension(ext, clipboardItems));
        }

        private void SetUpExtensions(IEnumerable<IFlowUIExtension> extensions)
        {
            foreach (IFlowUIExtension extension in extensions
                .ToList() /* avoid 'collection was modified' if we pass _loadedExtensions as a parameter */)
            {
                if (extension is IConfigurationRecipient confRecipient)
                {
                    foreach (ISettingKey settingKey in _settingsService.GetSettingKeys(extension.GetType()))
                    {
                        try { confRecipient.PutSetting(settingKey, _settingsService.GetSetting(settingKey)); }
                        catch (Exception ex) { throw new ClipboxExtensionException(nameof(confRecipient.PutSetting), extension.GetType(), ex); }
                    }
                }

                if (extension is ISettingChangeRequestor settingChangeRequestor)
                {
                    settingChangeRequestor.RequestSettingChange += ConfRequestor_RequestSettingChange;
                }

                lock (_loadedExtensionsLockHandle)
                {
                    _loadedExtensions.Add(extension);
                }
            }
        }

        private void ShutDownExtensions(IEnumerable<IFlowUIExtension> extensions)
        {
            foreach (IFlowUIExtension extension in extensions.ToList())
            {
                if (extension is ISettingChangeRequestor settingChangeRequestor)
                {
                    settingChangeRequestor.RequestSettingChange -= ConfRequestor_RequestSettingChange;
                }

                lock (_loadedExtensionsLockHandle)
                {
                    _loadedExtensions.Remove(extension);

                    try { extension.Destroy(); }
                    catch (Exception ex) { throw new ClipboxExtensionException(nameof(extension.Destroy), extension.GetType(), ex); }
                }
            }
        }

        private void ConfRequestor_RequestSettingChange(object sender, RequestSettingChangeEventArgs e)
        {
            // save the setting
            _settingsService.SetSetting(e.SettingKey, e.Value);
            // update the setting in the extension
            IConfigurationRecipient ext = (IConfigurationRecipient)sender;
            ext.PutSetting(e.SettingKey, e.Value);
        }

        private bool CheckShouldLoadExtension(IFlowUIExtension extension, IEnumerable<IClipboardItem> clipboardItems)
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
            OnMonitorDimensionsAssigned(dimensions);
        }

        protected virtual void OnMonitorDimensionsAssigned(MonitorDimensions monitorDimensions)
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

            if (_loadedExtensions.Any())
            {
                ShutDownExtensions(_loadedExtensions);
            }

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
