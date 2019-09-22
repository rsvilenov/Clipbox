using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.ClipboardViewItemTile;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views
{
    public abstract class ClipboardViewControlBase : UserControl, IDisposable
    {
        public Dictionary<string, string> Parameters { get; private set; }
        
        public List<string> SelectedItemIds { get; protected set; }
        
        public Dictionary<string, List<int>> SelectedMultiItemsIds { get; protected set; }

        public event EventHandler<SelectionEventArgs> SelectionChanged;

        public event EventHandler<CombinabilityEventArgs> CombinabilityCheckRequested;
        
        public event EventHandler<HousekeepingEventArgs> HousekeepingRequested;

        private UIExtentionContainer _container;

        private ISettingsService _settingsService;

        private readonly List<UIExtention> _loadedExtentions;
        
        private double _currentDisplayDpiCalibratedHeight;

        private Dictionary<string, string> _localExtentionSettings;

        protected MonitorDimensions MonitorDimensions { get; private set; }


        private SlidingTimer _uiExtentionGenerationSlidingTimer;

        private ListView _mainListView;

        private Panel _uiExtentionContainer;

        private string _lastSelectedItemId;

        private bool _areLastSelectedItemsCombinable;
        
        private List<MultiItem> _loadedMultiItems;


        public double ClipboardViewItemMaxHeight
        {
            get { return (double)GetValue(ClipboardViewItemHeightProperty); }
            set { SetValue(ClipboardViewItemHeightProperty, value); }
        }
        
        public static readonly DependencyProperty ClipboardViewItemHeightProperty =
            DependencyProperty.Register(nameof(ClipboardViewItemMaxHeight), typeof(double), typeof(ClipboardViewControlBase), new PropertyMetadata(0d));




        public bool AreAnyItemsSelected
        {
            get { return (bool)GetValue(AreAnyItemsSelectedProperty); }
            set { SetValue(AreAnyItemsSelectedProperty, value); }
        }
        
        public static readonly DependencyProperty AreAnyItemsSelectedProperty =
            DependencyProperty.Register(nameof(AreAnyItemsSelected), typeof(bool), typeof(ClipboardViewControlBase), new PropertyMetadata(false));




        public bool AreAnySubItemsSelected
        {
            get { return (bool)GetValue(AreAnyMultiItemsSelectedProperty); }
            set { SetValue(AreAnyMultiItemsSelectedProperty, value); }
        }
        
        public static readonly DependencyProperty AreAnyMultiItemsSelectedProperty =
            DependencyProperty.Register(nameof(AreAnySubItemsSelected), typeof(bool), typeof(ClipboardViewControlBase), new PropertyMetadata(false));
        
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
            SelectedItemIds = new List<string>();
            SelectedMultiItemsIds = new Dictionary<string, List<int>>();
            
            _uiExtentionGenerationSlidingTimer = new SlidingTimer();
            _uiExtentionGenerationSlidingTimer.IntervalMs = LoadingOfUIExtensionsDelayMs;
            _uiExtentionGenerationSlidingTimer.Elapsed += _uiExtentionGenerationSlidingTimer_Elapsed;

            _loadedMultiItems = new List<MultiItem>();

            Loaded += ClipboardViewControlBase_Loaded;
        }

        private void ClipboardViewControlBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            _mainListView = GetMainListView();
            if (_mainListView == null)
            {
                throw new NullReferenceException($"{nameof(GetMainListView)} returned null.");
            }

            _uiExtentionContainer = GetUIExtentionContainer();
            if (_uiExtentionContainer == null)
            {
                throw new NullReferenceException($"{nameof(GetUIExtentionContainer)} returned null.");
            }

            _mainListView.SelectionChanged += _mainListView_SelectionChanged;
        }

        private void _mainListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lastSelectedItem = e.AddedItems.Cast<object>().LastOrDefault() as ClipboardViewItemWrapper;
            // set the value of _lastSelectedItemId if the item is a single item or a group (both are descendants of ClipboardViewItem)
            // but not a sub-item,which is of type ClipboardViewSubItem and for which setting of _lastSelectedItemId
            // is handled by MultiItemListView_SelectionChanged
            if (lastSelectedItem != null && lastSelectedItem.ViewItem is ClipboardViewItem)
            {
                _lastSelectedItemId = lastSelectedItem.ViewItem.ClipboardItemId;
                
            }
            
            PerformOnSelectedChangedOpreations();

            // if we select a main item and we are not hoding Ctrl down,
            // then we should unselect every subitem which is selected.
            if (lastSelectedItem != null)
            {
                if (!KeyboardUtils.IsKeyPressed(Win32.Native.NativeEnums.Keys.LeftControl)
                && !KeyboardUtils.IsKeyPressed(Win32.Native.NativeEnums.Keys.RightContol))
                {
                    UnselectMultiItemSubItems();
                }
            }
        }
        
        private void _uiExtentionGenerationSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            (Action)(() =>
            {
                if (!IsLoadingOfUIExtensionsEnabled)
                {
                    AreAnyUIExtensionsLoaded = false;
                    // re-enable the loading of the extensions
                    IsLoadingOfUIExtensionsEnabled = true;
                    return;
                }

                List<IClipboardItem> selectedClipboardItems = _mainListView
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
            }));

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

        protected virtual void OnSelectedItemsCombinabilityChanged(SelectedItemsCombinabilityArgs args)
        { }
        
        protected void PerformOnSelectedChangedOpreations()
        {
            System.Diagnostics.Debug.WriteLine("SelectionChanged");

            if (_lastSelectedItemId == null)
            {
                return;
            }

            IEnumerable<ClipboardViewItemWrapper> selectedItems = _mainListView.SelectedItems.Cast<ClipboardViewItemWrapper>();

            // Due to the fact that we raise the selection and click events programmatically, we are limited to using MainListView_SelectionChanged.
            // MainListView.Items.CurrentChanging would have been of great help in cancelling the selection, but unfortunately we cannot use it here.
            var allSelectedItemIds = selectedItems.Select(item => item.Id).Union(SelectedMultiItemsIds.Keys);

            var combinabilityEventArgs = new CombinabilityEventArgs(selectedItems.Select(item => item.Id).ToList(), SelectedMultiItemsIds);

            SelectionChanged?.Invoke(this, combinabilityEventArgs);

            bool areSelectedItemsCombinable = true;

            if (combinabilityEventArgs.SelectedItemIds.Union(combinabilityEventArgs.SelectedMultiItemIds.Keys).Count() > 1)
            {
                CombinabilityCheckRequested?.Invoke(this, combinabilityEventArgs);
                areSelectedItemsCombinable = combinabilityEventArgs.AreItemsCombinable;
            }

            if (areSelectedItemsCombinable != _areLastSelectedItemsCombinable)
            {
                SelectedItemsCombinabilityArgs args = new SelectedItemsCombinabilityArgs(areSelectedItemsCombinable);

                OnSelectedItemsCombinabilityChanged(args);

                if (args.CancelLastSelection)
                {
                    ClipboardViewItemWrapper latestSelectedItem = _mainListView
                        .Items
                        .Cast<ClipboardViewItemWrapper>()
                        .Single(item => item.Id == _lastSelectedItemId);

                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
                    {
                        if (latestSelectedItem != null)
                        {
                            _mainListView.SelectedItems.Remove(latestSelectedItem);

                            // trigger the animation which shows that the item cannot be selected
                            latestSelectedItem.ViewItem.RunItemAnimation();
                        }
                    }));
                    return;
                }
                else
                {
                    _areLastSelectedItemsCombinable = areSelectedItemsCombinable;
                }
            }

            foreach (ClipboardViewItemWrapper item in _mainListView
                .Items
                .Cast<ClipboardViewItemWrapper>())
            {
                item.IsSelected = false;
            }

            SelectedItemIds.Clear();

            SelectedItemIds.AddRange(selectedItems.Select(item => item.Id));

            foreach (ClipboardViewItemWrapper item in selectedItems)
            {
                item.IsSelected = true;
            }

            //ClipboardViewItemWrapper lastSelectedItem = _mainListView.SelectedItems.Cast<ClipboardViewItemWrapper>().LastOrDefault();
            //if (lastSelectedItem != null)
            //{
            //    lastSelectedItem.IsSelected = true;
            //}

            UpdateAreAnyItemsSelected();
            UpdateAreAnySubItemsSelected();
            
            LoadUIExtensions();

#if DEBUG
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif

        }

        /// <summary>
        /// This property resets its state after the ui extension loading has been attempted.
        /// So if it is set to false, upon the attempt to load the ui extensions no extension will be
        /// loaded and the property will be set to true.
        /// </summary>
        protected bool IsLoadingOfUIExtensionsEnabled { get; set; }

        protected void LoadUIExtensions()
        {
            _uiExtentionContainer.Children.Clear();
            AreAnyUIExtensionsLoaded = false;
            if (SelectedItemIds.Any())
            {
                _uiExtentionGenerationSlidingTimer.Slide();
            }
        }

        private void MultiItem_SubItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("MultiItemListView_SelectionChanged");
            MultiItem multiItemUserControl = (MultiItem)sender;
            ListView multiItemListView = multiItemUserControl.GetSubItemsListView();

            var mainViewItem = multiItemUserControl.ClipboardViewItem;
            if (mainViewItem == null)
            {
                return;
            }

            ClipboardViewItemWrapper mainViewItemWrapper = _mainListView.Items.Cast<ClipboardViewItemWrapper>().
                        SingleOrDefault(item => item.ViewItem == mainViewItem);
            if (mainViewItemWrapper == null)
            {
                throw new InvalidOperationException("The selected item is not present in the ClipboardViewItemWrappers collection in the ViewModel.");
            }

            _lastSelectedItemId = mainViewItem?.ClipboardItemId;

            IEnumerable<ClipboardViewSubItemBase> selectedItems = multiItemListView.SelectedItems.Cast<ClipboardViewSubItemBase>();
            IEnumerable<int> selectedIndices = selectedItems.Select(item => multiItemListView.Items.IndexOf(item));

            SelectedMultiItemsIds.Remove(mainViewItem.ClipboardItemId);
            if (selectedItems.Any())
            {
                if (mainViewItemWrapper.IsSelected)
                {
                    _mainListView.SelectedItems.Remove(mainViewItemWrapper);
                    mainViewItemWrapper.IsSelected = false;
                }

                SelectedMultiItemsIds.Add(mainViewItem.ClipboardItemId, selectedIndices.ToList());
            }

            IEnumerable<ClipboardViewItemWrapper> mainListViewSelectedItems = _mainListView.SelectedItems.Cast<ClipboardViewItemWrapper>();


            //// Due to the fact that we raise the selection and click events programmatically, we are limited to using MainListView_SelectionChanged.
            //// MainListView.Items.CurrentChanging would have been of great help in cancelling the selection, but unfortunately we cannot use it here.
            
            bool areSelectedItemsCombinable = true;

            var combinabilityEventArgs = new CombinabilityEventArgs(mainListViewSelectedItems.Select(item => item.Id).ToList(), SelectedMultiItemsIds);

            SelectionChanged?.Invoke(this, combinabilityEventArgs);
            
            if (combinabilityEventArgs.SelectedItemIds.Union(combinabilityEventArgs.SelectedMultiItemIds.Keys).Count() > 1)
            {
                CombinabilityCheckRequested?.Invoke(this, combinabilityEventArgs);
                areSelectedItemsCombinable = combinabilityEventArgs.AreItemsCombinable;
            }
            
            if (areSelectedItemsCombinable != _areLastSelectedItemsCombinable)
            {
                SelectedItemsCombinabilityArgs args = new SelectedItemsCombinabilityArgs(areSelectedItemsCombinable);
                
                OnSelectedItemsCombinabilityChanged(args);

                if (args.CancelLastSelection)
                {
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
                    {
                        multiItemListView.UnselectAll();
                        SelectedMultiItemsIds.Remove(mainViewItem.ClipboardItemId);
                    }));
                    return;
                }
                else
                {
                    _areLastSelectedItemsCombinable = areSelectedItemsCombinable;
                }
            }

            foreach (ClipboardViewSubItemBase item in selectedItems)
            {
                var subItem = item as ClipboardViewSubItem;
                if (subItem != null)
                {
                    subItem.IsSelected = true;
                }
            }

            // if we are not holding Ctrl down, then upon selection of a subitem,
            // we should unselect every main item which is selected.
            if (!KeyboardUtils.IsKeyPressed(Win32.Native.NativeEnums.Keys.LeftControl)
                && !KeyboardUtils.IsKeyPressed(Win32.Native.NativeEnums.Keys.RightContol)
                && selectedItems.Any())
            {
                _mainListView.UnselectAll();
            }

            UpdateAreAnyItemsSelected();
            UpdateAreAnySubItemsSelected();
        }

        private void UpdateAreAnyItemsSelected()
        {
            AreAnyItemsSelected = SelectedItemIds.Any() || SelectedMultiItemsIds.Any();
        }

        private void UpdateAreAnySubItemsSelected()
        {
            AreAnySubItemsSelected = SelectedMultiItemsIds.Any();
        }

        private void MultiItem_Loaded(object sender, RoutedEventArgs e)
        {
            _loadedMultiItems.Add((MultiItem)sender);
        }

        private void MultiItem_Unloaded(object sender, RoutedEventArgs e)
        {
            MultiItem multiItem = (MultiItem)sender;

            multiItem.SubItems_SelectionChanged -= MultiItem_SubItems_SelectionChanged;
            multiItem.Unloaded -= MultiItem_Unloaded;
            multiItem.Loaded -= MultiItem_Loaded;

            _loadedMultiItems.Remove(multiItem);
        }

        protected void UnselectMultiItemSubItems()
        {
            foreach (MultiItem multiItem in _loadedMultiItems)
            {
                multiItem.GetSubItemsListView()?.UnselectAll();
            }
        }

        protected void PerformHousekeeping(IEnumerable<string> clipboardItemIdsToProcess, out IReadOnlyCollection<string> removedClipboardItemIds)
        {
            HousekeepingEventArgs args = new HousekeepingEventArgs(clipboardItemIdsToProcess);
            HousekeepingRequested?.Invoke(this, args);
            removedClipboardItemIds = args.RemovedClipboardItemIds.ToList().AsReadOnly();
        }

        protected abstract ListView GetMainListView();

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
                _uiExtentionGenerationSlidingTimer.Dispose();
                _mainListView.SelectionChanged -= _mainListView_SelectionChanged;
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
