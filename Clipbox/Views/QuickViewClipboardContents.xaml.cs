using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.ViewModels.Enums;
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
        private QuickViewkClipboardContentsViewModel _viewModel;
        
        private MonitorDimensions _oldMonitorDimensions;

        private ISettingsService _settingsService;

        public bool DimNonFocusedItems
        {
            get { return (bool)GetValue(DimNonFocusedItemsProperty); }
            set { SetValue(DimNonFocusedItemsProperty, value); }
        }
        
        public static readonly DependencyProperty DimNonFocusedItemsProperty =
            DependencyProperty.Register(nameof(DimNonFocusedItems), typeof(bool), typeof(QuickViewClipboardContents), new PropertyMetadata(false));

        private bool _isDisposing;

        public QuickViewClipboardContents(QuickViewkClipboardContentsViewModel vm, UIExtentionContainer container, ISettingsService settingsService)
            : base (container, settingsService)
        {   
            InitializeComponent();

            mainGrid.DataContext = vm;
            _viewModel = vm;

            _settingsService = settingsService;

            _settingsService.SettingChanged += _settingsService_SettingChanged;
            DimNonFocusedItems = _settingsService.GetSetting<bool>(SettingsKeys.DimNonFocusedItems);
            
            IsVisibleChanged += QuickViewClipboardContents_IsVisibleChanged;

            MainListView.UncombinableItemsSelected += MainListView_UncombinableItemsSelected;
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

            MainListView.RunAnimationForItem(wrapper, AnimationPurpose.Error);

            int errorTextDurationMs = TileAnimationHelper.COLOR_ANIMATION_DURATION_MS + 700;
            ShowErrorTextTemporarily("Incompatible items.", errorTextDurationMs);
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

        

        private void _settingsService_SettingChanged(object sender, SettingsEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
            {
                DimNonFocusedItems = _settingsService.GetSetting<bool>(SettingsKeys.DimNonFocusedItems);
            }));
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

        private void ShowErrorTextTemporarily(string text, int visibleMilliseconds)
        {
            uiExtentionContainer.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Visibility = Visibility.Visible;
            ErrorTextBlock.Text = text;
            Task.Delay(visibleMilliseconds).ContinueWith(t =>
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
                {
                    ErrorTextBlock.Text = string.Empty;
                    ErrorTextBlock.Visibility = Visibility.Collapsed;
                    uiExtentionContainer.Visibility = Visibility.Visible;
                }));
            });
        }

        private void ButtonClearItems_Click(object sender, RoutedEventArgs e)
        {
            // Make sure no item remains in these lists when the listview is cleared of all its items
           
        }
        
        protected override void OnSetControlMaxSizeRequested(MonitorDimensions monitorDimensions)
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
        }
        
        protected override Panel GetUIExtentionContainer()
        {
            return uiExtentionContainer;
        }

        private void MainListView_ItemPinUnpinRequested(object sender, EventArgsClasses.ClipboardItemManipulationEventArgs e)
        {
            _viewModel.PinUnpinClipboardEntry(e.ClipboardItemId);
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

            List<IClipboardItem> selectedClipboardItems = MainListView
                .SelectedItems
                .Cast<ClipboardViewItemWrapper>()
                .Select(w => w.ClipboardItem)
                .ToList();

            IEnumerable<ClipboardViewItemWrapper> multiItemItemWrappers = MainListView
                .Items
                .Cast<ClipboardViewItemWrapper>()
                .Where(item => MainListView.GetSelectedMultiItemsIds().Keys.Contains(item.Id));

            selectedClipboardItems.AddRange(multiItemItemWrappers.Select(w => w.ClipboardItem));

            base.LoadUIExtensions(selectedClipboardItems);
        }

        protected override void Dispose(bool disposing)
        {
            _isDisposing = true;

            _viewModel.Dispose();
            base.Dispose(disposing);
        }
    }
}
