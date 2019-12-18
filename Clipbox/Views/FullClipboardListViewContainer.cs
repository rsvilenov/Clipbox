using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Infrastructure.UI.Windows.Enums;
using Clipbox.Services.EventArgsClasses;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.ClipboardListView;
using Clipbox.Views.Enums.ClipboardViewItemTile;
using Clipbox.Views.EventArgsClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipbox.Views
{
    public class FullClipboardListViewContainer : FrameworkElement, IDisposable
    {
        private AcryllicWindow _listViewContainerWindow;
        private FrameworkElement _containerFrameworkElement;
        private FullClipboardListView _fullClipboardListView;
        private readonly ManualResetEvent _listViewIinstantiationMre;

        private bool _isDisposed;

        public OwnThreadDispatcher InnerThreadDispatcher { get; }

        public Dispatcher ContainerDispatcher { get; }

        #region FullClipboardListView proxy events
        public event EventHandler<ClipboardItemManipulationEventArgs> ViewItemLoadingRequested;

        public event EventHandler<ClipboardItemManipulationEventArgs> ViewItemUnloadingRequested;

        public event EventHandler<ClipboardItemManipulationEventArgs> ViewItemLoadingCancelRequested;

        public event EventHandler<EventArgs> GroupingComplete;

        public event EventHandler<KeyEventArgs> ClipboardItemsPanel_KeyUp;

        public event EventHandler<EventArgs> ClipboardItemContentControl_MouseDoubleClickDetectionStarted;

        public event EventHandler<EventArgs> ClipboardItemContentControl_MouseDoubleClickDetection_DoubleClickDetected;

        public event EventHandler<EventArgs> ClipboardItemContentControl_MouseDoubleClickDetection_SingleClickDetected;

        public event EventHandler<EventArgs> LoadMoreItemsRequested;

        public event EventHandler<CombinabilityEventArgs> CombinabilityCheckRequested;

        public event EventHandler<ClipboardItemManipulationEventArgs> ItemDeletionRequested;

        public event EventHandler<ClipboardItemManipulationEventArgs> ItemRemovalRequested;

        public event EventHandler<ClipboardItemManipulationEventArgs> ItemPinUnpinRequested;

        public event EventHandler<ClipboardItemManipulationEventArgs> ItemEnableDisableSyncRequested;

        public event EventHandler<SelectionEventArgs> SelectionChangeCompleted;

        #endregion

        #region proxy DPs
        public bool AreAnyItemsSelected
        {
            get { return (bool)GetValue(AreAnyItemsSelectedProperty); }
            protected set { SetValue(AreAnyItemsSelectedPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey AreAnyItemsSelectedPropertyKey
        = DependencyProperty.RegisterReadOnly(nameof(AreAnyItemsSelected), typeof(bool), typeof(FullClipboardListViewContainer),
            new FrameworkPropertyMetadata(default(bool),
                FrameworkPropertyMetadataOptions.None));

        public static readonly DependencyProperty AreAnyItemsSelectedProperty
            = AreAnyItemsSelectedPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey AreAnySubItemsSelectedPropertyKey
        = DependencyProperty.RegisterReadOnly(nameof(AreAnySubItemsSelected), typeof(bool), typeof(FullClipboardListViewContainer),
            new FrameworkPropertyMetadata(default(bool),
                FrameworkPropertyMetadataOptions.None));

        public static readonly DependencyProperty AreAnySubItemsSelectedProperty
            = AreAnySubItemsSelectedPropertyKey.DependencyProperty;

        /// <summary>
        /// A read-only DP, indicating whether there are any selected sub items.
        /// </summary>
        public bool AreAnySubItemsSelected
        {
            get { return (bool)GetValue(AreAnySubItemsSelectedProperty); }
            protected set { SetValue(AreAnySubItemsSelectedPropertyKey, value); }
        }


        public bool SuppressCustomSelectionProcessing
        {
            get { return (bool)GetValue(SuppressCustomSelectionProcessingProperty); }
            set { SetValue(SuppressCustomSelectionProcessingProperty, value); }
        }

        public static readonly DependencyProperty SuppressCustomSelectionProcessingProperty =
            DependencyProperty.Register(nameof(SuppressCustomSelectionProcessing), typeof(bool), typeof(FullClipboardListViewContainer), new PropertyMetadata(false, SuppressCustomSelectionProcessing_PropertyChanged));

        private static void SuppressCustomSelectionProcessing_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FullClipboardListView lv = ((FullClipboardListViewContainer)d)._fullClipboardListView;
            lv?.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => lv.SetValue(FullClipboardListView.SuppressCustomSelectionProcessingProperty, e.NewValue)));
        }

        public bool AreSelectedItemsCombinable
        {
            get { return (bool)GetValue(AreSelectedItemsCombinableProperty); }
            set { SetValue(AreSelectedItemsCombinableProperty, value); }
        }

        public static readonly DependencyProperty AreSelectedItemsCombinableProperty =
            DependencyProperty.Register(nameof(AreSelectedItemsCombinable), typeof(bool), typeof(FullClipboardListViewContainer), new PropertyMetadata(true));
        
        public bool IsCloudSyncAvailable
        {
            get { return (bool)GetValue(IsCloudSyncAvailableProperty); }
            set { SetValue(IsCloudSyncAvailableProperty, value); }
        }

        public static readonly DependencyProperty IsCloudSyncAvailableProperty =
            DependencyProperty.Register(nameof(IsCloudSyncAvailable), typeof(bool), typeof(FullClipboardListViewContainer), new PropertyMetadata(false, IsCloudSyncAvailable_PropertyChanged));

        private static void IsCloudSyncAvailable_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FullClipboardListView lv = ((FullClipboardListViewContainer)d)._fullClipboardListView;
            lv?.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => lv.SetValue(FullClipboardListView.IsCloudSyncAvailableProperty, e.NewValue)));
        }

        public bool IsFadeInOfItemsEnabled
        {
            get { return (bool)GetValue(IsFadeInOfItemsEnabledProperty); }
            set { SetValue(IsFadeInOfItemsEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsFadeInOfItemsEnabledProperty =
            DependencyProperty.Register(nameof(IsFadeInOfItemsEnabled), typeof(bool), typeof(FullClipboardListViewContainer), new PropertyMetadata(false, IsFadeInOfItemsEnabled_PropertyChanged));

        private static void IsFadeInOfItemsEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FullClipboardListView lv = ((FullClipboardListViewContainer)d)._fullClipboardListView;
            lv?.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => lv.SetValue(FullClipboardListView.IsFadeInOfItemsEnabledProperty, e.NewValue)));
        }

        public int NumOfNotYetVisibleRowsToPreload
        {
            get { return (int)GetValue(NumOfNotYetVisibleRowsToPreloadProperty); }
            set { SetValue(NumOfNotYetVisibleRowsToPreloadProperty, value); }
        }

        public static readonly DependencyProperty NumOfNotYetVisibleRowsToPreloadProperty =
            DependencyProperty.Register(nameof(NumOfNotYetVisibleRowsToPreload), typeof(int), typeof(FullClipboardListViewContainer), new PropertyMetadata(0, NumOfNotYetVisibleRowsToPreload_PropertyChanged));

        private static void NumOfNotYetVisibleRowsToPreload_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FullClipboardListView lv = ((FullClipboardListViewContainer)d)._fullClipboardListView;
            lv?.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => lv.SetValue(FullClipboardListView.NumOfNotYetVisibleRowsToPreloadProperty, e.NewValue)));
        }

        public bool IsInnerGroupingEnabled
        {
            get { return (bool)GetValue(IsInnerGroupingEnabledProperty); }
            set { SetValue(IsInnerGroupingEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsInnerGroupingEnabledProperty =
            DependencyProperty.Register(nameof(IsInnerGroupingEnabled), typeof(bool), typeof(FullClipboardListViewContainer), new PropertyMetadata(false, IsInnerGroupingEnabled_PropertyChanged));

        private static void IsInnerGroupingEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FullClipboardListView lv = ((FullClipboardListViewContainer)d)._fullClipboardListView;
            lv?.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => lv.SetValue(FullClipboardListView.IsInnerGroupingEnabledProperty, e.NewValue)));
        }
        
        public int TotalItemsCountToLoad
        {
            get { return (int)GetValue(TotalItemsCountToLoadProperty); }
            set { SetValue(TotalItemsCountToLoadProperty, value); }
        }

        public static readonly DependencyProperty TotalItemsCountToLoadProperty =
            DependencyProperty.Register(nameof(TotalItemsCountToLoad), typeof(int), typeof(FullClipboardListViewContainer), new PropertyMetadata(0, TotalItemsCountToLoad_PropertyChanged));

        private static void TotalItemsCountToLoad_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FullClipboardListView lv = ((FullClipboardListViewContainer)d)._fullClipboardListView;
            lv?.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => lv.SetValue(FullClipboardListView.TotalItemsCountToLoadProperty, e.NewValue)));
        }

        public double ClipboardViewItemMaxHeight
        {
            get { return (double)GetValue(ClipboardViewItemHeightProperty); }
            set { SetValue(ClipboardViewItemHeightProperty, value); }
        }


        public static readonly DependencyProperty ClipboardViewItemHeightProperty =
            DependencyProperty.Register(nameof(ClipboardViewItemMaxHeight), typeof(double), typeof(FullClipboardListViewContainer), new PropertyMetadata(0d, ClipboardViewItemHeight_PropertyChanged));

        private static void ClipboardViewItemHeight_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FullClipboardListView lv = ((FullClipboardListViewContainer)d)._fullClipboardListView;
            lv?.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => lv.SetValue(ClipboardListViewBase.ClipboardViewItemHeightProperty, e.NewValue)));
        }

        public IEnumerable<ClipboardViewItemWrapper> ItemsSource
        {
            get { return (IEnumerable<ClipboardViewItemWrapper>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<ClipboardViewItemWrapper>), typeof(FullClipboardListViewContainer), new PropertyMetadata(null, ItemsSource_PropertyChanged));

        private static void ItemsSource_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FullClipboardListViewContainer lvc = (FullClipboardListViewContainer)d;
            lvc?.SetContainerDpValueToListView(ItemsSourceProperty, FullClipboardListView.ItemsSourceProperty);
        }


        #endregion

        public FullClipboardListViewContainer()
        {
            _listViewIinstantiationMre = new ManualResetEvent(false);

            InnerThreadDispatcher = new OwnThreadDispatcher();

            ContainerDispatcher = Dispatcher;

            InnerThreadDispatcher.BeginInvoke(DispatcherPriority.Send,
               new Action(() =>
               {
                   _fullClipboardListView = new FullClipboardListView
                   {
                       Background = Brushes.Gainsboro,
                       Visibility = Visibility.Visible,
                       HorizontalAlignment = HorizontalAlignment.Stretch,
                       VerticalAlignment = VerticalAlignment.Stretch,
                       BorderBrush = Brushes.Red,
                       BorderThickness = new Thickness(2)
                   };

                   DependencyPropertyDescriptor
                        .FromProperty(FullClipboardListView.AreSelectedItemsCombinableProperty, typeof(FullClipboardListView))
                        .AddValueChanged(_fullClipboardListView, ListView_AreSelectedItemsCombinableProperty_PropertyChanged);
                   DependencyPropertyDescriptor
                        .FromProperty(ClipboardListViewBase.AreAnyItemsSelectedProperty, typeof(ClipboardListViewBase))
                        .AddValueChanged(_fullClipboardListView, ListView_AreAnyItemsSelectedProperty_PropertyChanged);
                   DependencyPropertyDescriptor
                        .FromProperty(ClipboardListViewBase.AreAnySubItemsSelectedProperty, typeof(ClipboardListViewBase))
                        .AddValueChanged(_fullClipboardListView, ListView_AreAnySubItemsSelectedProperty_PropertyChanged);

                   _fullClipboardListView.ClipboardItemContentControl_MouseDoubleClickDetectionStarted += _fullClipboardListView_ClipboardItemContentControl_MouseDoubleClickDetectionStarted;
                   _fullClipboardListView.ClipboardItemContentControl_MouseDoubleClickDetection_DoubleClickDetected += _fullClipboardListView_ClipboardItemContentControl_MouseDoubleClickDetection_DoubleClickDetected;
                   _fullClipboardListView.ClipboardItemContentControl_MouseDoubleClickDetection_SingleClickDetected += _fullClipboardListView_ClipboardItemContentControl_MouseDoubleClickDetection_SingleClickDetected;
                   _fullClipboardListView.ClipboardItemsPanel_KeyUp += _fullClipboardListView_ClipboardItemsPanel_KeyUp;
                   _fullClipboardListView.CombinabilityCheckRequested += _fullClipboardListView_CombinabilityCheckRequested;
                   _fullClipboardListView.ItemDeletionRequested += _fullClipboardListView_ItemDeletionRequested;
                   _fullClipboardListView.ItemEnableDisableSyncRequested += _fullClipboardListView_ItemEnableDisableSyncRequested;
                   _fullClipboardListView.ItemPinUnpinRequested += _fullClipboardListView_ItemPinUnpinRequested;
                   _fullClipboardListView.ItemRemovalRequested += _fullClipboardListView_ItemRemovalRequested;
                   _fullClipboardListView.LoadMoreItemsRequested += _fullClipboardListView_LoadMoreItemsRequested;
                   _fullClipboardListView.GroupingComplete += _fullClipboardListView_GroupingComplete;
                   _fullClipboardListView.ViewItemLoadingCancelRequested += _fullClipboardListView_ViewItemLoadingCancelRequested;
                   _fullClipboardListView.ViewItemLoadingRequested += _fullClipboardListView_ViewItemLoadingRequested;
                   _fullClipboardListView.ViewItemUnloadingRequested += _fullClipboardListView_ViewItemUnloadingRequested;

                   _listViewIinstantiationMre.Set();

                   _listViewContainerWindow = new AcryllicWindow
                   {
                       ScaleMode = DpiScaleMode.Custom,
                       BorderBrush = Brushes.Red,
                       BorderThickness = new Thickness(2),
                       WindowStartupLocation = WindowStartupLocation.Manual,
                       WindowStyle = WindowStyle.None,
                       ResizeMode = ResizeMode.NoResize,
                       Background = Brushes.Transparent,
                       ShowInTaskbar = false,
                       Content = _fullClipboardListView
                   };
                   
               }));

            Loaded += FullClipboardListViewContainer_Loaded;
            Unloaded += FullClipboardListViewContainer_Unloaded;
        }

        #region event routing (event handlers)

        private void _fullClipboardListView_ViewItemLoadingRequested(object sender, ClipboardItemManipulationEventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ViewItemLoadingRequested?.Invoke(this, e)));
        }

        private void _fullClipboardListView_ViewItemUnloadingRequested(object sender, ClipboardItemManipulationEventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ViewItemUnloadingRequested?.Invoke(this, e)));
        }

        private void _fullClipboardListView_ViewItemLoadingCancelRequested(object sender, ClipboardItemManipulationEventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ViewItemLoadingCancelRequested?.Invoke(this, e)));
        }

        private void _fullClipboardListView_GroupingComplete(object sender, EventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                GroupingComplete?.Invoke(this, e)));
        }

        private void _fullClipboardListView_LoadMoreItemsRequested(object sender, EventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                LoadMoreItemsRequested?.Invoke(this, e)));
        }

        private void _fullClipboardListView_ItemRemovalRequested(object sender, ClipboardItemManipulationEventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ItemRemovalRequested?.Invoke(this, e)));
        }

        private void _fullClipboardListView_ItemPinUnpinRequested(object sender, ClipboardItemManipulationEventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ItemPinUnpinRequested?.Invoke(this, e)));
        }

        private void _fullClipboardListView_ItemEnableDisableSyncRequested(object sender, ClipboardItemManipulationEventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ItemEnableDisableSyncRequested?.Invoke(this, e)));
        }

        private void _fullClipboardListView_ItemDeletionRequested(object sender, ClipboardItemManipulationEventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ItemDeletionRequested?.Invoke(this, e)));
        }

        private void _fullClipboardListView_CombinabilityCheckRequested(object sender, Services.EventArgsClasses.CombinabilityEventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                CombinabilityCheckRequested?.Invoke(this, e)));
        }

        private void _fullClipboardListView_ClipboardItemsPanel_KeyUp(object sender, KeyEventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ClipboardItemsPanel_KeyUp?.Invoke(this, e)));
        }

        private void _fullClipboardListView_ClipboardItemContentControl_MouseDoubleClickDetection_SingleClickDetected(object sender, EventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ClipboardItemContentControl_MouseDoubleClickDetection_SingleClickDetected?.Invoke(this, e)));
        }

        private void _fullClipboardListView_ClipboardItemContentControl_MouseDoubleClickDetection_DoubleClickDetected(object sender, EventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ClipboardItemContentControl_MouseDoubleClickDetection_DoubleClickDetected?.Invoke(this, e)));
        }

        private void _fullClipboardListView_ClipboardItemContentControl_MouseDoubleClickDetectionStarted(object sender, EventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                ClipboardItemContentControl_MouseDoubleClickDetectionStarted?.Invoke(this, e)));
        }

        private void _fullClipboardListView_SelectionChangeCompleted(object sender, SelectionEventArgs e)
        {
            ContainerDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                SelectionChangeCompleted?.Invoke(this, e)));
        }

        #endregion

        #region property change routing
        private void ListView_AreAnySubItemsSelectedProperty_PropertyChanged(object sender, EventArgs e)
        {
            SetListViewDpValueToContainer(FullClipboardListView.AreAnySubItemsSelectedProperty, FullClipboardListViewContainer.AreAnySubItemsSelectedProperty);
        }

        private void ListView_AreAnyItemsSelectedProperty_PropertyChanged(object sender, EventArgs e)
        {
            SetListViewDpValueToContainer(FullClipboardListView.AreAnyItemsSelectedProperty, FullClipboardListViewContainer.AreAnyItemsSelectedProperty);
        }

        private void ListView_AreSelectedItemsCombinableProperty_PropertyChanged(object sender, EventArgs e)
        {
            SetListViewDpValueToContainer(FullClipboardListView.AreSelectedItemsCombinableProperty, FullClipboardListViewContainer.AreSelectedItemsCombinableProperty);
        }

        public void RunAnimationForItem(ClipboardViewItemWrapper wrapper, AnimationPurpose purpose)
        {
            InnerThreadDispatcher.Invoke(DispatcherPriority.Send, new Action(() => 
                _fullClipboardListView.RunAnimationForItem(wrapper, purpose)));
        }

        public IReadOnlyCollection<string> GetSelectedItemIds()
        {
            return InnerThreadDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                _fullClipboardListView.GetSelectedItemIds())) as IReadOnlyCollection<string>;
        }

        public IReadOnlyDictionary<string, IReadOnlyCollection<int>> GetSelectedMultiItemsIds()
        {
            return InnerThreadDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                _fullClipboardListView.GetSelectedMultiItemsIds())) as IReadOnlyDictionary<string, IReadOnlyCollection<int>>;
        }

        public List<ClipboardViewItemWrapper> GetSelectedMainItemsIncludingMultiItems()
        {
            return InnerThreadDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                _fullClipboardListView.GetSelectedMainItemsIncludingMultiItems())) as List<ClipboardViewItemWrapper>;
        }

        #endregion


        #region proxy methods routing
        public void UnselectAll()
        {
            InnerThreadDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                _fullClipboardListView.UnselectAll()));
        }

        public void UnselectMultiItemSubItems()
        {
            InnerThreadDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                _fullClipboardListView.UnselectMultiItemSubItems()));
        }

        public void SetItemToAlert(ClipboardViewItemWrapper wrapper)
        {
            InnerThreadDispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                _fullClipboardListView.SetItemToAlert(wrapper)));
        }
        #endregion

        public int DoubleClickDetectionWaitIntervalMs
        {
            get { return _fullClipboardListView.DoubleClickDetectionWaitIntervalMs; }
        }

        public FullClipboardListView InnerListView
        {
            get { return _fullClipboardListView; }
        }

        private void FullClipboardListViewContainer_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }


        AsyncObservableCollection<ClipboardViewItemWrapper> _containerItemsSource;
        AsyncObservableCollection<ClipboardViewItemWrapper> _lvItemsSource;


        private void SetListViewDpValueToContainer(DependencyProperty sourceDp, DependencyProperty destDp)
        {
            InnerThreadDispatcher.BeginInvoke(DispatcherPriority.Send,
                new Action(() =>
                {
                    var value = _fullClipboardListView.GetValue(sourceDp);

                    if (value is IEnumerable)
                    {
                        throw new NotSupportedException("Collections are not supported");
                    }

                    ContainerDispatcher.BeginInvoke(DispatcherPriority.Send,
                        new Action(() => this.SetValue(destDp, value)));
                }));
        }

        private void SetContainerDpValueToListView(DependencyProperty sourceDp, DependencyProperty destDp)
        {
            ContainerDispatcher.BeginInvoke(DispatcherPriority.Send,
                new Action(() =>
                {
                    var value = this.GetValue(sourceDp);

                    if (value is AsyncObservableCollection<ClipboardViewItemWrapper> wrapperCollection)
                    {
                        if (_containerItemsSource != null)
                        {
                            _containerItemsSource.CollectionChanged -= ContainerItemsSource_CollectionChanged;
                        }
                        _containerItemsSource = wrapperCollection;
                        _containerItemsSource.CollectionChanged += ContainerItemsSource_CollectionChanged;
                        _lvItemsSource = new AsyncObservableCollection<ClipboardViewItemWrapper>(wrapperCollection);
                        _lvItemsSource.EnableSyncForCurrentUIThread();
                        InnerThreadDispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => _lvItemsSource.EnableSyncForCurrentUIThread()));
                        value = _lvItemsSource;
                    }

                    InnerThreadDispatcher.BeginInvoke(DispatcherPriority.Send,
                        new Action(() => _fullClipboardListView.SetValue(destDp, value)));
                }));
        }

        private void ContainerItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_lvItemsSource != null)
            {
                switch(e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        _lvItemsSource.AddRange(e.NewItems.Cast<ClipboardViewItemWrapper>().ToList());
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        _lvItemsSource.RemoveRange(e.NewItems.Cast<ClipboardViewItemWrapper>().ToList());
                        break;
                    default:
                        _lvItemsSource.Clear();
                        _lvItemsSource = new AsyncObservableCollection<ClipboardViewItemWrapper>(_containerItemsSource);
                        ItemsSource = _lvItemsSource;
                        break;
                }
            }
        }

        private void FullClipboardListViewContainer_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= FullClipboardListViewContainer_Loaded;

            _listViewIinstantiationMre.WaitOne();
            SetContainerDpValueToListView(AreSelectedItemsCombinableProperty, FullClipboardListView.AreSelectedItemsCombinableProperty);
            SetContainerDpValueToListView(ClipboardViewItemHeightProperty, ClipboardListViewBase.ClipboardViewItemHeightProperty);
            SetContainerDpValueToListView(IsCloudSyncAvailableProperty, FullClipboardListView.IsCloudSyncAvailableProperty);
            SetContainerDpValueToListView(IsFadeInOfItemsEnabledProperty, FullClipboardListView.IsFadeInOfItemsEnabledProperty);
            SetContainerDpValueToListView(IsInnerGroupingEnabledProperty, FullClipboardListView.IsInnerGroupingEnabledProperty);
            SetContainerDpValueToListView(NumOfNotYetVisibleRowsToPreloadProperty, FullClipboardListView.NumOfNotYetVisibleRowsToPreloadProperty);
            SetContainerDpValueToListView(TotalItemsCountToLoadProperty, FullClipboardListView.TotalItemsCountToLoadProperty);
            SetContainerDpValueToListView(ItemsSourceProperty, ItemsControl.ItemsSourceProperty);
            
            if (Parent is FrameworkElement containerFrameworkElement)
            {
                _containerFrameworkElement = containerFrameworkElement;
                GetContainerBounds(out Point containerScreenCoordinates, out Point containerScreenSize);
                _containerFrameworkElement.LayoutUpdated += _containerFrameworkElement_LayoutUpdated;
                _containerFrameworkElement.Unloaded += _containerFrameworkElement_Unloaded;

                InnerThreadDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send,
                new Action(() =>
                {
                    _listViewContainerWindow.Left = containerScreenCoordinates.X;
                    _listViewContainerWindow.Top = containerScreenCoordinates.Y;
                    _listViewContainerWindow.Width = containerScreenSize.X - containerScreenCoordinates.X;
                    _listViewContainerWindow.Height = containerScreenSize.Y - containerScreenCoordinates.Y;
                }));
            }
            else
            {
                throw new InvalidOperationException("Parent not found.");
            }

            IntPtr parentHwnd = IntPtr.Zero;
            if (Window.GetWindow(this) is BaseWindow parentWindow)
            {
                parentHwnd = parentWindow.Handle;
            }

            InnerThreadDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send,
            new Action(() =>
            {
                _listViewContainerWindow.AllowsTransparency = true;
                _listViewContainerWindow.Background = Brushes.Transparent;
                _listViewContainerWindow.Show();

                if (parentHwnd != IntPtr.Zero)
                {
                    Win32.Native.User32.SetParent(_listViewContainerWindow.Handle, parentHwnd);
                }
            }));
        }

        private void _containerFrameworkElement_Unloaded(object sender, RoutedEventArgs e)
        {
            _containerFrameworkElement.LayoutUpdated -= _containerFrameworkElement_LayoutUpdated;
            _containerFrameworkElement.Unloaded -= _containerFrameworkElement_Unloaded;
            
            _containerItemsSource.CollectionChanged -= ContainerItemsSource_CollectionChanged;
        }

        private void _containerFrameworkElement_LayoutUpdated(object sender, EventArgs e)
        {
            GetContainerBounds(out Point containerScreenCoordinates, out Point containerScreenSize);
            InnerThreadDispatcher.BeginInvoke(DispatcherPriority.Send,
            new Action(() =>
            {
                _listViewContainerWindow.Left = containerScreenCoordinates.X;
                _listViewContainerWindow.Top = containerScreenCoordinates.Y;
                _listViewContainerWindow.Width = containerScreenSize.X - containerScreenCoordinates.X;
                _listViewContainerWindow.Height = containerScreenSize.Y - containerScreenCoordinates.Y;
            }));
        }

        private void GetContainerBounds(out Point containerScreenCoordinates, out Point containerScreenSize)
        {
            containerScreenCoordinates = default(Point);
            containerScreenSize = default(Point);

            if (_containerFrameworkElement.IsVisible) // avoid the "This Visual is not connected to a PresentationSource." InvalidOperationException
            {
                try
                {
                    PresentationSource source = PresentationSource.FromVisual(_containerFrameworkElement);

                    if (source != null)
                    {
                        Point locationFromScreen = _containerFrameworkElement.PointToScreen(new Point(0, 0));
                        Point parentSize = _containerFrameworkElement.PointToScreen(new Point(_containerFrameworkElement.ActualWidth, _containerFrameworkElement.ActualHeight));

                        // Transform screen point to WPF device independent point
                        containerScreenCoordinates = source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);
                        containerScreenSize = source.CompositionTarget.TransformFromDevice.Transform(parentSize);
                    }
                }
                catch (InvalidOperationException)
                {
                    // if this exception still happens, ignore it. There is at least one more reason for PointToScreen to
                    // throw this exception (we can see its source in Visual.cs), but... so much is for now.
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Unloaded -= FullClipboardListViewContainer_Unloaded;

                _listViewIinstantiationMre.Dispose();

                InnerThreadDispatcher.BeginInvoke(DispatcherPriority.Send,
                    new Action(() =>
                    {
                        DependencyPropertyDescriptor
                            .FromProperty(FullClipboardListView.AreSelectedItemsCombinableProperty, typeof(FullClipboardListView))
                            .RemoveValueChanged(_fullClipboardListView, ListView_AreSelectedItemsCombinableProperty_PropertyChanged);
                        DependencyPropertyDescriptor
                             .FromProperty(ClipboardListViewBase.AreAnyItemsSelectedProperty, typeof(ClipboardListViewBase))
                             .RemoveValueChanged(_fullClipboardListView, ListView_AreAnyItemsSelectedProperty_PropertyChanged);
                        DependencyPropertyDescriptor
                             .FromProperty(ClipboardListViewBase.AreAnySubItemsSelectedProperty, typeof(ClipboardListViewBase))
                             .RemoveValueChanged(_fullClipboardListView, ListView_AreAnySubItemsSelectedProperty_PropertyChanged);

                        _fullClipboardListView.Dispose();
                        _listViewContainerWindow.Close();
                    }))
                    .Wait();

                InnerThreadDispatcher.Dispose();
            }
        }
    }
}
