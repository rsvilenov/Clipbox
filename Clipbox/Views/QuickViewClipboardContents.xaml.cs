using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.ClipboardViewItemTile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for QuickViewClipboardContents.xaml
    /// </summary>
    public partial class QuickViewClipboardContents : ClipboardViewControlBase, IDisposable
    {
        private QuicViewkClipboardContentsViewModel _viewModel;

        private string _lastSelectedItemId;

        private MonitorDimensions _oldMonitorDimensions;

        private ISettingsService _settingsService;

        private SlidingTimer _uiExtentionGenerationSlidingTimer;
        
        public bool DimNonFocusedItems
        {
            get { return (bool)GetValue(DimNonFocusedItemsProperty); }
            set { SetValue(DimNonFocusedItemsProperty, value); }
        }
        
        public static readonly DependencyProperty DimNonFocusedItemsProperty =
            DependencyProperty.Register(nameof(DimNonFocusedItems), typeof(bool), typeof(QuickViewClipboardContents), new PropertyMetadata(false));
        
        public QuickViewClipboardContents(QuicViewkClipboardContentsViewModel vm, UIExtentionContainer container, ISettingsService settingsService)
            : base (container, settingsService)
        {   
            InitializeComponent();

            mainGrid.DataContext = vm;
            _viewModel = vm;

            _settingsService = settingsService;

            _settingsService.SettingChanged += _settingsService_SettingChanged;
            DimNonFocusedItems = _settingsService.GetSetting<bool>(SettingsKeys.DimNonFocusedItems);

            _uiExtentionGenerationSlidingTimer = new SlidingTimer();
            _uiExtentionGenerationSlidingTimer.IntervalMs = 200d;
            _uiExtentionGenerationSlidingTimer.Elapsed += _uiExtentionGenerationSlidingTimer_Elapsed;
            
            MainListView.SelectionChanged += MainListView_SelectionChanged;
            IsVisibleChanged += QuickViewClipboardContents_IsVisibleChanged;
        }

        private void QuickViewClipboardContents_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue && base.MonitorDimensions != _oldMonitorDimensions)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
                {
                    LoadItemsAndViewItems();
                }));

                _oldMonitorDimensions = base.MonitorDimensions;
            }
        }

        private void _uiExtentionGenerationSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            (Action)(() =>
            {
                List<IClipboardItem> selectedClipboardItems = MainListView
                .SelectedItems
                .Cast<ClipboardViewItemWrapper>()
                .Select(w => w.ClipboardItem)
                .ToList();

                var allExtentions = base.GetExtentions(selectedClipboardItems);

                foreach (var extention in allExtentions)
                {
                    UIElement extentionUIElement = null;
                    extention.OnExtentionViewGenerating(selectedClipboardItems, ref extentionUIElement, new Size(uiExtentionContainer.MaxWidth, uiExtentionContainer.MaxHeight));
                    uiExtentionContainer.Children.Add(extentionUIElement);
                }
            }));

        }

        private void _settingsService_SettingChanged(object sender, SettingsEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
            {
                DimNonFocusedItems = _settingsService.GetSetting<bool>(SettingsKeys.DimNonFocusedItems);
            }));
        }

        private void LoadItemsAndViewItems()
        {
            _viewModel.ClipboardViewItemWrappers.EnableSyncForCurrentUIThread();

            Task.Run(new Action(() => _viewModel.ReloadWrappedItems(MonitorDimensions)))
                .ContinueWith((t) => _viewModel.LoadViewItems(MonitorDimensions));
        }
        
        private void MainListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
        
        private void PerformOnSelectedChangedOpreations()
        {
            System.Diagnostics.Debug.WriteLine("SelectionChanged");

            if (_lastSelectedItemId == null)
            {
                return;
            }

            IEnumerable<ClipboardViewItemWrapper> selectedItems = MainListView.SelectedItems.Cast<ClipboardViewItemWrapper>();

            // Due to the fact that we raise the selection and click events programmatically, we are limited to using MainListView_SelectionChanged.
            // MainListView.Items.CurrentChanging would have been of great help in cancelling the selection, but unfortunately we cannot use it here.
            var allSelectedItemIds = selectedItems.Select(item => item.Id).Union(SelectedMultiItemsIds.Keys);
            SelectionChangedEventAnswer eventAnswer = RaiseSelectionChangedEvent(allSelectedItemIds);
            if (eventAnswer.IsCancelled)
            {
                ClipboardViewItemWrapper latestSelectedItem = _viewModel.ClipboardViewItemWrappers.Single(item => item.Id == _lastSelectedItemId);
                //if (latestSelectedItem is ClipboardViewItemGroup == false)
                {
                    CancelSelectionWithMessage(latestSelectedItem, eventAnswer.CancelMessage);
                    return;
                }
            }

            foreach (ClipboardViewItemWrapper item in _viewModel.SelectedClipboardViewItemWrappers)
            {
                item.ViewItem.IsSelected = false;
            }

            _viewModel.SelectedClipboardViewItemWrappers.Clear();
            SelectedItemIds.Clear();

            _viewModel.SelectedClipboardViewItemWrappers.AddRange(selectedItems);
            SelectedItemIds.AddRange(selectedItems.Select(item => item.Id));

            foreach (ClipboardViewItemWrapper item in selectedItems)
            {
                item.ViewItem.IsSelected = true;
            }

            uiExtentionContainer.Children.Clear();
            if (selectedItems.Any())
            {
                _uiExtentionGenerationSlidingTimer.Slide();

                //List<IClipboardItem> originalClipboardItems = new List<IClipboardItem>();
                //foreach (string id in selectedItems.Select(item => item.ClipboardItemId))
                //{
                //    IClipboardItem item = _viewModel.GetClipboardItem(id, restoreShrinked: false);
                //    originalClipboardItems.Add(item);
                //}
                
                //Task.Factory.StartNew(() =>
                //{
                //    var allExtentions = base.GetExtentions(originalClipboardItems);
                //    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                //        (Action)(() =>
                //        {
                //            foreach (var extention in allExtentions)
                //            {
                //                UIElement extentionUIElement = null;
                //                extention.OnExtentionViewGenerating(originalClipboardItems, ref extentionUIElement, new Size(uiExtentionContainer.MaxWidth, uiExtentionContainer.MaxHeight));
                //                uiExtentionContainer.Children.Add(extentionUIElement);
                //            }
                //        }));

                //});


            }

            ClipboardViewItemWrapper lastSelectedItem = MainListView.SelectedItems.Cast<ClipboardViewItemWrapper>().LastOrDefault();
            if (lastSelectedItem != null)
            {
                lastSelectedItem.ViewItem.IsSelected = true;
            }

#if DEBUG
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif

        }

        public override void FocusNextItem()
        {
            // changing the selected index will not cause SelectionChanged to fire, but assigning it for the first time will fire the event
            if (MainListView.SelectedIndex == -1)
            {
                MainListView.SelectedIndex = 0;
            }
            else
            {
                if (MainListView.SelectedIndex == MainListView.Items.Count - 1)
                {
                    MainListView.SelectedIndex = 0;
                }
                else
                {
                    MainListView.SelectedIndex++;
                }

                PerformOnSelectedChangedOpreations();
            }
            
        }

        public override void ResumeDefaultState()
        {
            MainListView.UnselectAll();
            _viewModel.SelectedClipboardViewItemWrappers.Clear();
            SelectedItemIds.Clear();
            SelectedMultiItemsIds.Clear();
        }

        public override void CancelSelectionWithMessage(ClipboardViewItemWrapper item, string message)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
            {
                if (item != null)
                {
                    MainListView.SelectedItems.Remove(item);

                    // trigger the animation which shows that the item cannot be selected
                    item.ViewItem.SignalThatItemIsUnselectable();

                    ShowErrorTextTemporarily(message, 500);
                }
            }));
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

        private void MultiItem_SubItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("MultiItemListView_SelectionChanged");
            MultiItem multiItemUserControl = (MultiItem)sender;
            ListView listView = multiItemUserControl.GetSubItemsListView();
            
            var mainViewItem = multiItemUserControl.ClipboardViewItem;
            if (mainViewItem == null)
            {
                return;
            }

            ClipboardViewItemWrapper mainViewItemWrapper = _viewModel.ClipboardViewItemWrappers.
                        SingleOrDefault(item => item.ViewItem == mainViewItem);
            if (mainViewItemWrapper == null)
            {
                throw new InvalidOperationException("The selected item is not present in the ClipboardViewItemWrappers collection in the ViewModel.");
            }

            _lastSelectedItemId = mainViewItem?.ClipboardItemId;

            IEnumerable<ClipboardViewSubItemBase> selectedItems = listView.SelectedItems.Cast<ClipboardViewSubItemBase>();
            IEnumerable<int> selectedIndices = selectedItems.Select(item => listView.Items.IndexOf(item));

            SelectedMultiItemsIds.Remove(mainViewItem.ClipboardItemId);
            if (selectedItems.Any())
            {
                if (mainViewItem.IsSelected)
                {
                    MainListView.SelectedItems.Remove(mainViewItemWrapper);
                    mainViewItem.IsSelected = false;
                }

                SelectedMultiItemsIds.Add(mainViewItem.ClipboardItemId, selectedIndices.ToList());
            }
            
            IEnumerable<ClipboardViewItemWrapper> mainListViewSelectedItems = MainListView.SelectedItems.Cast<ClipboardViewItemWrapper>();


            //// Due to the fact that we raise the selection and click events programmatically, we are limited to using MainListView_SelectionChanged.
            //// MainListView.Items.CurrentChanging would have been of great help in cancelling the selection, but unfortunately we cannot use it here.

            var selectedItemsPlusMainItem = mainListViewSelectedItems.Select(item => item.Id)
                .Concat(new List<string> { mainViewItem.ClipboardItemId });

            SelectionChangedEventAnswer eventAnswer = RaiseSelectionChangedEvent(selectedItemsPlusMainItem);
            if (eventAnswer.IsCancelled)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
                {
                    MainListView.SelectedItems.Remove(mainViewItemWrapper);

                    // trigger the animation which shows that the item cannot be selected
                    mainViewItem.SignalThatItemIsUnselectable();
                    listView.UnselectAll();
                    SelectedMultiItemsIds.Remove(mainViewItem.ClipboardItemId);

                    ShowErrorTextTemporarily(eventAnswer.CancelMessage, 500);
                }));
                return;
            }

            //foreach (ClipboardViewItem item in _viewModel.SelectedClipboardViewItems)
            //{
            //    item.IsSelected = false;
            ////}

            //_viewModel.SelectedClipboardViewItems.Clear();
            //SelectedItemIds.Clear();

            //_viewModel.SelectedClipboardViewItems.AddRange(selectedItems);
            //SelectedItemIds.AddRange(selectedItems.Select(item => item.ClipboardItemId));
            
            foreach (ClipboardViewSubItemBase item in selectedItems)
            {
                var subItem = item as ClipboardViewSubItem;
                if(subItem != null)
                {
                    subItem.IsSelected = true;
                }
            }

            //uiExtentionContainer.Children.Clear();
            //if (selectedItems.Any())
            //{
            //    IReadOnlyCollection<IClipboardItem> originalClipboardItems = _viewModel.GetOriginalClipboardItemsForSelection();
            //    var allExtentions = base.GetExtentions(originalClipboardItems);
            //    foreach (var extention in allExtentions)
            //    {
            //        UIElement extentionUIElement = null;
            //        extention.OnViewGenerating(originalClipboardItems, ref extentionUIElement, new Size(uiExtentionContainer.MaxWidth, uiExtentionContainer.MaxHeight));
            //        uiExtentionContainer.Children.Add(extentionUIElement);
            //    }
            //}

            //ClipboardViewItem lastSelectedItem = MainListView.SelectedItems.Cast<ClipboardViewItem>().LastOrDefault();
            //if (lastSelectedItem != null)
            //{
            //    lastSelectedItem.IsSelected = true;
            //}
        }

        private void Grid_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {

            }
        }

        private void Grid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }

        private void ButtonClearItems_Click(object sender, RoutedEventArgs e)
        {
            // Make sure no item remains in these lists when the listview is cleared of all its items
            SelectedItemIds.Clear();
            SelectedMultiItemsIds.Clear();
        }

        public void Dispose()
        {
            _uiExtentionGenerationSlidingTimer.Dispose();
        }

        private void MultiItem_Unloaded(object sender, RoutedEventArgs e)
        {
            MultiItem multiItem = (MultiItem)sender;

            multiItem.SubItems_SelectionChanged -= MultiItem_SubItems_SelectionChanged;
            multiItem.Unloaded -= MultiItem_Unloaded;
        }
    }
}
