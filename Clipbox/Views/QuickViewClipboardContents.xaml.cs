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
    public partial class QuickViewClipboardContents : ClipboardViewControlBase
    {
        private QuickViewkClipboardContentsViewModel _viewModel;

        private string _lastSelectedItemId;

        private MonitorDimensions _oldMonitorDimensions;

        private ISettingsService _settingsService;

        public bool DimNonFocusedItems
        {
            get { return (bool)GetValue(DimNonFocusedItemsProperty); }
            set { SetValue(DimNonFocusedItemsProperty, value); }
        }
        
        public static readonly DependencyProperty DimNonFocusedItemsProperty =
            DependencyProperty.Register(nameof(DimNonFocusedItems), typeof(bool), typeof(QuickViewClipboardContents), new PropertyMetadata(false));
        

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

            //Task.Run(new Action(() => LoadWrappedItems()))
            //    .ContinueWith((t) => _viewModel.LoadViewItems(MonitorDimensions));
            Task.Run(new Action(() => _viewModel.LoadViewItems(MonitorDimensions)));
        }

        public void LoadWrappedItems()
        {
            _viewModel.ReloadWrappedItems(MonitorDimensions);
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

                base.PerformOnSelectedChangedOpreations();
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

        protected override ListView GetMainListView()
        {
            return MainListView;
        }

        protected override Panel GetUIExtentionContainer()
        {
            return uiExtentionContainer;
        }
    }
}
