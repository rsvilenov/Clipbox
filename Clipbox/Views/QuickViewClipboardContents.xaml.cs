using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.ClipboardViewItemTile;
using Clipbox.Views.Enums.ClipboardViewItemTile;
using Clipbox.Views.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for QuickViewClipboardContents.xaml
    /// </summary>
    public partial class QuickViewClipboardContents : ClipboardViewControlBase
    {
        private const string STORED_STATE_PERSISTENCE_KEY = "QuickViewState";

        private readonly QuickViewkClipboardContentsViewModel _viewModel;
        private readonly ISettingsService _settingsService;
        private readonly IPersistenceProvider _persistenceProvider;

        private MonitorDimensions _oldMonitorDimensions;
        private bool _isDisposing;

        public QuickViewClipboardContents(QuickViewkClipboardContentsViewModel vm,
            UIExtensionContainer container, 
            ISettingsService settingsService,
            IPersistenceProvider persistenceProvider)
            : base (container, settingsService)
        {   
            InitializeComponent();

            mainGrid.DataContext = vm;
            _viewModel = vm;

            _settingsService = settingsService;
            _persistenceProvider = persistenceProvider;

            IsVisibleChanged += QuickViewClipboardContents_IsVisibleChanged;

            _settingsService.SettingChanged += _settingsService_SettingChanged;

            DimNonFocusedItems = settingsService.GetSetting<bool>(SettingsKeys.DimNonFocusedItemsInQuickView);

            MainListView.UncombinableItemsSelected += MainListView_UncombinableItemsSelected;
        }

        private void _settingsService_SettingChanged(object sender, SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.DimNonFocusedItemsInQuickView)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
                {
                    DimNonFocusedItems = _settingsService.GetSetting<bool>(SettingsKeys.DimNonFocusedItemsInQuickView);
                }));
            }
        }

        private void MainListView_UncombinableItemsSelected(object sender, UncombinableItemsEventArgs e)
        {
            ClipboardViewItemWrapper wrapper = MainListView.Items
                    .Cast<ClipboardViewItemWrapper>()
                    .SingleOrDefault(w => w.Id == e.LastSelectedItemId);
            
            if (wrapper == null)
            {
                throw new InvalidOperationException($"Either no items are selected or the selected items are not of type {nameof(ClipboardViewItemWrapper)}");
            }

            if (!(MainListView.ItemContainerGenerator.ContainerFromItem(wrapper) is ListViewItem selectedListViewItem))
            {
                throw new InvalidOperationException($"The list view does not contain an item with id {wrapper.Id}.");
            }

            var clipboardViewItemTile = Traversing.GetVisualChildOfType<ItemPresenterBase>(selectedListViewItem);
            if (!(clipboardViewItemTile is IAnimatableItem animatableItem))
            {
                throw new InvalidOperationException($"The listviewitem for {wrapper.Id} does not implement {nameof(IAnimatableItem)}");
            }

            animatableItem.RunNotificationAnimation(AnimationPurpose.Error);

            int errorTextDurationMs = TileAnimationHelper.COLOR_ANIMATION_DURATION_MS + 700;
            ShowIncompatibilityErrorTextTemporarily(errorTextDurationMs);
        }

        private void QuickViewClipboardContents_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                var previousStage = _viewModel.ClipboardItemsLoadingStage;
                
                _viewModel.ClipboardViewItemWrappers.EnableSyncForCurrentUIThread();

                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                {
                    Task.Run(new Action(() =>
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (_isDisposing)
                            {
                                return;
                            }

                            try
                            {
                                base.PerformHousekeeping(_viewModel.ClipboardViewItemWrappers.Select(w => w.Id),
                                    out IReadOnlyCollection<string> removedClipboardItemIds);

                                _viewModel.RemoveClipboardItemWrappers(removedClipboardItemIds);
                            }
                            catch
                            {
                                if (!_isDisposing)
                                {
                                    throw;
                                }
                            }
                        }));
                    }))
                    .ContinueWith((t) =>
                    {
                        if (_isDisposing)
                        {
                            return;
                        }

                        try
                        {
                            if (base.MonitorDimensions != _oldMonitorDimensions)
                            {
                                _oldMonitorDimensions = base.MonitorDimensions;
                                LoadViewItems();
                            }
                        }
                        catch
                        {
                            if (!_isDisposing)
                            {
                                throw;
                            }
                        }
                    });
                }));
            }
        }

        private void LoadViewItems()
        {
            _viewModel.LoadViewItems(MonitorDimensions);
        }

        public void LoadWrappedPinnedItems()
        {
            _viewModel.LoadWrappedPinnedItems(MonitorDimensions);
        }

        public void LoadItem(string clipboardItemId)
        {
            _viewModel.ScheduleLoadingOfNewItem(clipboardItemId);
        }

        public void StoreState()
        {
            if (_viewModel.ClipboardViewItemWrappers.Any())
            {
                string stateString = new string(_viewModel
                    .ClipboardViewItemWrappers
                    .SelectMany(w => w.Id + ";").ToArray());
                _persistenceProvider.Write(STORED_STATE_PERSISTENCE_KEY, stateString);
            }
        }

        public bool CheckIsStoredStateAvailable()
        {
            return _persistenceProvider.CheckIfKeyExists(STORED_STATE_PERSISTENCE_KEY);
        }

        public void RestoreState()
        {
            if (_persistenceProvider.CheckIfKeyExists(STORED_STATE_PERSISTENCE_KEY))
            {
                string stateString = _persistenceProvider.ReadText(STORED_STATE_PERSISTENCE_KEY);
                if (!string.IsNullOrEmpty(stateString))
                {
                    foreach (string itemId in stateString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _viewModel.ScheduleLoadingOfNewItem(itemId);
                    }
                }

                _persistenceProvider.Delete(STORED_STATE_PERSISTENCE_KEY);
            }
        }
        
        public override void FocusNextItem()
        {
            MainListView.FocusNextItem();
        }

        public override void ResumeDefaultState()
        {
            MainListView.UnselectAll();
            MainListView.UnselectMultiItemSubItems();
            _viewModel.SelectedClipboardViewItemWrappers.Clear();
        }

        private void ShowIncompatibilityErrorTextTemporarily(int visibleMilliseconds)
        {
            uiExtensionContainer.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Visibility = Visibility.Visible;
            Task.Delay(visibleMilliseconds).ContinueWith(t =>
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
                {
                    ErrorTextBlock.Visibility = Visibility.Collapsed;
                    uiExtensionContainer.Visibility = Visibility.Visible;
                }));
            });
        }

        private void ButtonClearItems_Click(object sender, RoutedEventArgs e)
        {
            // Make sure no item remains in these lists when the listview is cleared of all its items
           
        }
        
        protected override void OnMonitorDimensionsAssigned(MonitorDimensions monitorDimensions)
        {
            double currentDisplayHeight = monitorDimensions.WorkAreaHeight;
            double currentDisplayWidth = monitorDimensions.WorkAreaWidth;
            
            // display bounds - 20%
            double controlMaxWidth = currentDisplayWidth - (currentDisplayWidth * 0.2);
            double controlMaxHeight = currentDisplayHeight - (currentDisplayHeight * 0.2);

            // we should not dpi-scale these dimensions because the UserControl itself
            // should not be resized in DpiScaleMode.Custom dpi mode.
            this.MaxWidth = controlMaxWidth;
            this.MaxHeight = controlMaxHeight;

            _viewModel.SetDefaultMonitorDimensions(monitorDimensions);
        }
        
        protected override Panel GetUIExtensionContainer()
        {
            return uiExtensionContainer;
        }

        private void MainListView_ItemPinUnpinRequested(object sender, EventArgsClasses.ClipboardItemManipulationEventArgs e)
        {
            _viewModel.PinUnpinClipboardEntry(e.ClipboardItemId);
        }

        private void MainListView_ItemCanPinRequested(object sender, ClipboardItemManipulationCancelEventArgs e)
        {
            e.Cancel = !_viewModel.CanPinUnpinItem(e.ClipboardItemId);
        }

        private void MainListView_ItemRemovalRequested(object sender, EventArgsClasses.ClipboardItemManipulationEventArgs e)
        {
            _viewModel.RemoveClipboardItemWrappers(new string[] { e.ClipboardItemId });
        }

        public IReadOnlyCollection<string> GetSelectedItemIds()
        {
            return MainListView.GetSelectedItemIds();
        }


        public IReadOnlyDictionary<string, IReadOnlyCollection<int>> GetSelectedMultiItemsIds()
        {
            return MainListView.GetSelectedMultiItemsIds();
        }

        private void MainListView_CombinabilityCheckRequested(object sender, CombinabilityEventArgs args)
        {
            RaiseCombinabilityCheckRequested(args);
        }
        
        private void MainListView_SelectionChangeCompleted(object sender, SelectionEventArgs e)
        {
            base.ClearLoadedUIExtensions();
            base.LoadUIExtensions(MainListView);
        }

        protected override void Dispose(bool disposing)
        {
            _isDisposing = true;

            _viewModel.Dispose();
            base.Dispose(disposing);
        }
    }
}
