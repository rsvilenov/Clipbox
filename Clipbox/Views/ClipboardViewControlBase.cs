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



        public ClipboardViewControlBase(UIExtentionContainer container, ISettingsService settingsService)
        {
            _container = container;
            _settingsService = settingsService;
            _loadedExtentions = new List<UIExtention>();
            _localExtentionSettings = new Dictionary<string, string>();
            SelectedItemIds = new List<string>();
            SelectedMultiItemsIds = new Dictionary<string, List<int>>();
            
            _uiExtentionGenerationSlidingTimer = new SlidingTimer();
            _uiExtentionGenerationSlidingTimer.IntervalMs = 200d;
            _uiExtentionGenerationSlidingTimer.Elapsed += _uiExtentionGenerationSlidingTimer_Elapsed;

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
        }
        
        private void _uiExtentionGenerationSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            (Action)(() =>
            {
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

        public virtual void CancelSelectionWithMessage(ClipboardViewItemWrapper item, string message)
        { }

        public virtual void ResumeDefaultState()
        { }

        public virtual void FocusNextItem()
        { }


        protected SelectionChangedEventAnswer RaiseSelectionChangedEvent(IEnumerable<string> selectedItems)
        {
            var eventArgs = new SelectionEventArgs
            {
                SelectedItemIds = selectedItems
            };

            SelectionChanged?.Invoke(this, eventArgs);

            return new SelectionChangedEventAnswer(eventArgs.AreItemsCombinable);
        }

        protected class SelectionChangedEventAnswer
        {
            public SelectionChangedEventAnswer(bool areItemsCompatible)
            {
                AreItemsCombinable = areItemsCompatible;
            }

            public bool AreItemsCombinable { get; }
        }

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
            SelectionChangedEventAnswer eventAnswer = RaiseSelectionChangedEvent(allSelectedItemIds);
            if (allSelectedItemIds.Count() > 1 && !eventAnswer.AreItemsCombinable)
            {
                ClipboardViewItemWrapper latestSelectedItem = _mainListView
                    .Items
                    .Cast<ClipboardViewItemWrapper>()
                    .Single(item => item.Id == _lastSelectedItemId);
                //if (latestSelectedItem is ClipboardViewItemGroup == false)
                {
                    CancelSelectionWithMessage(latestSelectedItem, "Incompatible items.");
                    return;
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

            _uiExtentionContainer.Children.Clear();
            if (selectedItems.Any())
            {
                _uiExtentionGenerationSlidingTimer.Slide();
            }

            ClipboardViewItemWrapper lastSelectedItem = _mainListView.SelectedItems.Cast<ClipboardViewItemWrapper>().LastOrDefault();
            if (lastSelectedItem != null)
            {
                lastSelectedItem.IsSelected = true;
            }

            UpdateAreAnyItemsSelected();

#if DEBUG
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif

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

            var selectedItemsPlusMainItem = mainListViewSelectedItems.Select(item => item.Id)
                .Concat(new List<string> { mainViewItem.ClipboardItemId });

            SelectionChangedEventAnswer eventAnswer = RaiseSelectionChangedEvent(selectedItemsPlusMainItem);
            if (selectedItemsPlusMainItem.Count() > 1 && !eventAnswer.AreItemsCombinable)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
                {
                    CancelSelectionWithMessage(mainViewItemWrapper, "Incompatible items.");
                    
                    multiItemListView.UnselectAll();
                    SelectedMultiItemsIds.Remove(mainViewItem.ClipboardItemId);
                }));
                return;
            }

            foreach (ClipboardViewSubItemBase item in selectedItems)
            {
                var subItem = item as ClipboardViewSubItem;
                if (subItem != null)
                {
                    subItem.IsSelected = true;
                }
            }

            UpdateAreAnyItemsSelected();
        }

        private void UpdateAreAnyItemsSelected()
        {
            AreAnyItemsSelected = SelectedItemIds.Any() || SelectedMultiItemsIds.Any();
        }

        protected void MultiItem_Unloaded(object sender, RoutedEventArgs e)
        {
            MultiItem multiItem = (MultiItem)sender;

            multiItem.SubItems_SelectionChanged -= MultiItem_SubItems_SelectionChanged;
            multiItem.Unloaded -= MultiItem_Unloaded;
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
    }
}
