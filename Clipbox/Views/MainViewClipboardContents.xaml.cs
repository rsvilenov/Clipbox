using Clipbox.Core.Extensibility.Enums;
using Clipbox.Core.Search;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Theme.Enums;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Services;
using Clipbox.Services.Catalogues;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.ClipboardViewItemTile;
using Clipbox.Views.Dialog.EventArgsClasses;
using Clipbox.Views.Enums.ClipboardViewItemTile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for MainViewClipboardContents.xaml
    /// </summary>
    public partial class MainViewClipboardContents : ClipboardViewControlBase, IDisposable
    {
        const int HINT_POPUP_DELAY_MS = 1000;

        private readonly ISettingsService _settingsService;
        private readonly IUpdateNotificationService _updateNotificationService;
        private readonly IPopupHintService _popupHintService;

        private readonly int _maxNumOfItemsToLoadAtOnce;
        private readonly List<ISearchTerm> _searchTerms;
        private readonly CancellableTaskManager _cancellableTaskManager;
        private readonly SlidingTimer _uiExtensionGenerationSlidingTimer;
        private readonly SlidingTimer _hintPopupSlidingTimer;

        private MainViewClipboardContentsViewModel _viewModel;

        private string _lastShownHintPopupName;

        /// <summary>
        /// This field provides not a nice solution to the problem of
        /// running dispatcher actions or tasks, requiring resources,
        /// destroyed by Dispose(), which run after the Dispose() has started...
        /// </summary>
        private volatile bool _isDisposing;

        public event EventHandler<SetToClipboardEventArgs> SetItemsToClipboardRequested;
        public event EventHandler CloseMainWindowRequested;
        public event EventHandler<SettingsWindowEventArgs> SettingsWindowOpenRequested;
        public event EventHandler RebuildIndexRequested;

        public MainViewClipboardContents(MainViewClipboardContentsViewModel vm,
            UIExtensionContainer container,
            ISettingsService settingsService,
            IUpdateNotificationService updateNotificationService,
            IPopupHintService popupHintService)
            : base(container, settingsService)
        {
            InitializeComponent();
            _viewModel = vm;
            DataContext = _viewModel;

            _settingsService = settingsService;
            _updateNotificationService = updateNotificationService;
            _popupHintService = popupHintService;

            _cancellableTaskManager = new CancellableTaskManager();

            _uiExtensionGenerationSlidingTimer = new SlidingTimer
            {
                IntervalMs = LoadingOfUIExtensionsDelayMs
            };
            _uiExtensionGenerationSlidingTimer.Elapsed += _uiExtensionGenerationSlidingTimer_Elapsed;

            _hintPopupSlidingTimer = new SlidingTimer
            {
                IntervalMs = HINT_POPUP_DELAY_MS
            };
            _hintPopupSlidingTimer.Elapsed += _hintPopupSlidingTimer_Elapsed;


            Loaded += MainViewClipboardContents_Loaded;
            IsVisibleChanged += MainViewClipboardContents_IsVisibleChanged;

            // this setting should not change while this view is open, so no SettingChanged subscription is needed.
            AreFadeAnimationsEnabled = settingsService.GetSetting<bool>(SettingsKeys.AreFadeAnimationsEnabled);


            int numOfNotYetVisibleRowsToPreload = settingsService.GetSetting<int>(SettingsKeys.NumOfNotYetVisibleRowsToPreload);

            if (numOfNotYetVisibleRowsToPreload < 0)
            {
                throw new InvalidOperationException($"Setting {nameof(SettingsKeys.NumOfNotYetVisibleRowsToPreload)} should be equal to 0 or greater than 0.");
            }

            NumOfNotYetVisibleRowsToPreload = numOfNotYetVisibleRowsToPreload;

            _maxNumOfItemsToLoadAtOnce = settingsService.GetSetting<int>(SettingsKeys.MaxNumOfItemToLoadAtOnce);

            if (_maxNumOfItemsToLoadAtOnce < 0)
            {
                throw new InvalidOperationException($"Setting {nameof(SettingsKeys.MaxNumOfItemToLoadAtOnce)} should be equal to 0 or greater than 0.");
            }

            DimNonFocusedItems = settingsService.GetSetting<bool>(SettingsKeys.DimNonFocusedItemsInMainView);

            _searchTerms = new List<ISearchTerm>();

            _viewModel.RequestCloseOfMainWindow += _viewModel_RequestCloseOfMainWindow;
            _settingsService.SettingChanged += SettingsService_SettingChanged;

            // this window is created anew on every openning, so no event subscription is needed here...
            IsUpdateAvailable = _updateNotificationService.IsUpdateAvailable;
        }

        private void _hintPopupSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
            (Action)(() =>
            {
                ShowHintPopup();
            }));
        }

        private void ShowHintPopup()
        {
            if (setToClipboardBtn.IsVisible
                && !_popupHintService.HasPopupBeenSeen(PopupHintNames.PP_HINT_CHARGE_CLIPBOARD))
            {
                _lastShownHintPopupName = PopupHintNames.PP_HINT_CHARGE_CLIPBOARD;
                _popupHintService.TryShowHintPopup(PopupHintNames.PP_HINT_CHARGE_CLIPBOARD, setToClipboardBtn, true);
            }
        }
            

        public bool AreFadeAnimationsEnabled
        {
            get { return (bool)GetValue(AreFadeAnimationsEnabledProperty); }
            set { SetValue(AreFadeAnimationsEnabledProperty, value); }
        }

        public static readonly DependencyProperty AreFadeAnimationsEnabledProperty =
            DependencyProperty.Register(nameof(AreFadeAnimationsEnabled), typeof(bool), typeof(MainViewClipboardContents), new PropertyMetadata(false));

        public bool IsShowingOfPostSelectionToolboxEnabled
        {
            get { return (bool)GetValue(IsLoadingOfPostSelectionToolboxEnabledProperty); }
            set { SetValue(IsLoadingOfPostSelectionToolboxEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsLoadingOfPostSelectionToolboxEnabledProperty =
            DependencyProperty.Register(nameof(IsShowingOfPostSelectionToolboxEnabled), typeof(bool), typeof(MainViewClipboardContents), new PropertyMetadata(false));


        public int NumOfNotYetVisibleRowsToPreload
        {
            get { return (int)GetValue(NumOfNotYetVisibleRowsToPreloadProperty); }
            set { SetValue(NumOfNotYetVisibleRowsToPreloadProperty, value); }
        }

        public static readonly DependencyProperty NumOfNotYetVisibleRowsToPreloadProperty =
            DependencyProperty.Register(nameof(NumOfNotYetVisibleRowsToPreload), typeof(int), typeof(MainViewClipboardContents), new PropertyMetadata(0));

        public int AllPagesItemsCount
        {
            get { return (int)GetValue(AllPagesItemsCountProperty); }
            set { SetValue(AllPagesItemsCountProperty, value); }
        }

        public static readonly DependencyProperty AllPagesItemsCountProperty =
            DependencyProperty.Register(nameof(AllPagesItemsCount), typeof(int), typeof(MainViewClipboardContents), new PropertyMetadata(0));


        public bool IsUpdateAvailable
        {
            get { return (bool)GetValue(IsUpdateAvailableProperty); }
            set { SetValue(IsUpdateAvailableProperty, value); }
        }

        public static readonly DependencyProperty IsUpdateAvailableProperty =
            DependencyProperty.Register(nameof(IsUpdateAvailable), typeof(bool), typeof(MainViewClipboardContents), new PropertyMetadata(false));


        private void _viewModel_RequestCloseOfMainWindow(object sender, EventArgs e)
        {
            CloseMainWindowRequested?.Invoke(this, new EventArgs());
        }

        private void SettingsService_SettingChanged(object sender, SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.DimNonFocusedItemsInMainView)
            {
                ISettingsService settingsService = (ISettingsService)sender;
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
                {
                    DimNonFocusedItems = settingsService.GetSetting<bool>(SettingsKeys.DimNonFocusedItemsInMainView);
                }));
            }
        }

        private void MainViewClipboardContents_Loaded(object sender, RoutedEventArgs e)
        {
            // we need to delay the loading of the UI extensions so that we can cancel it if 
            // the selection is after a double click. Double clicks should set the item to the
            // system clipboard and close the window, so UI extensions are not needed.
            base.LoadingOfUIExtensionsDelayMs += MainListView.DoubleClickDetectionWaitIntervalMs;
        }

        private void MainViewClipboardContents_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                LoadContents(0, _maxNumOfItemsToLoadAtOnce);
            }
        }

        private void LoadContents(int startIndex = -1, int count = -1, bool clearOldContents = false)
        {
            if (!(SynchronizationContext.Current is DispatcherSynchronizationContext))
            {
                throw new InvalidOperationException("This method should be called from the dispatcher thread only");
            }

            _viewModel.ClipboardViewItemWrappers.EnableSyncForCurrentUIThread();
            IEnumerable<string> clipboardItemIds = null;
            IReadOnlyCollection<string> removedClipboardItemIds = null;

            LoadingPopupGovernor.OpenPopup(Window.GetWindow(this));

            int allMatchingClipboardItemIdCount = 0;
            Task.Run(() =>
            {
                IEnumerable<string> allMatchingClipboardItemIds = null;
                PerformDisposeSafeOperation
                (
                    () => allMatchingClipboardItemIds = _viewModel.GetClipboardItemIds(_searchTerms) // maybe pass filters to this method later
                );

                allMatchingClipboardItemIdCount = allMatchingClipboardItemIds.Count();

                clipboardItemIds = allMatchingClipboardItemIds
                .Skip(startIndex == -1 ? 0 : startIndex)
                .Take(count == -1 ? allMatchingClipboardItemIdCount : count);

                base.PerformHousekeeping(clipboardItemIds, out removedClipboardItemIds);

            }).ContinueWith((t) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        if (_isDisposing)
                        {
                            return;
                        }

                        var itemsToLoad = clipboardItemIds.Except(removedClipboardItemIds);

                        // if there are no loaded items, we have to show the
                        // MainListViewContainer because its GroupingComplete event will
                        // not fire and the loading circle will not disappear
                        if (!itemsToLoad.Any())
                        {
                            _viewModel.ClipboardViewItemWrappers.Clear();

                            // close the loading popup
                            LoadingPopupGovernor.ClosePopup();

                            MainListViewContainer.Visibility = Visibility.Visible;
                            // we should also show the top and side bars, so that the
                            // user knows that what is shown on the screen is actually
                            // the application and not an empty desktop :)
                            TopBarGrid.Visibility = Visibility.Visible;
                            SideBarGrid.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            AllPagesItemsCount = allMatchingClipboardItemIdCount - removedClipboardItemIds.Count();

                            PerformDisposeSafeOperation
                            (
                                () =>
                                {
                                    if (clearOldContents)
                                    {
                                        _viewModel.ClipboardViewItemWrappers.Clear();
                                    }

                                    _viewModel.LoadAllItems(this.MonitorDimensions, itemsToLoad);
                                }
                            );
                        }

                        //#if DEBUG
                        //                        GC.Collect();
                        //                        GC.WaitForPendingFinalizers();
                        //                        GC.Collect();
                        //#endif
                    }));
            },
            // ensure that the code in ContinueWith executes on the main thread,
            // which should be the UI thread, so we have access to the correct dispatcher
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        

        public void ScheduleLoadingOfViewItem(string clipboardItemId)
        {
            // run the slow-running operations in a separate task
            _cancellableTaskManager.ScheduleTask(clipboardItemId, new Action<string>((taskId) =>
            {
                ClipboardViewItemWrapper wrapper = null;
                ClipboardViewItemDimensionInfo cvItemDimInfo = null;
                ClipboardViewItem viewItem = null;

                bool hasLoadingOfWrapperSucceded = true;
                PerformDisposeSafeOperation(() =>
                {
                    wrapper = _viewModel.GetViewItemWrapper(clipboardItemId);
                    if (wrapper == null)
                    {
                        // by the time this task executes, the item may has been filtered out by the search or the filters...
                        hasLoadingOfWrapperSucceded = false;
                    }
                    else
                    {
                        viewItem = _viewModel.GetViewItem(clipboardItemId, base.MonitorDimensions);
                        _viewModel.TryGetPreviewDimensions(base.MonitorDimensions, wrapper, out cvItemDimInfo);
                    }
                });

                if (hasLoadingOfWrapperSucceded)
                {
                    // this optimization (updating the bindable properties in the dispatcher thread with background priority)
                    // solves the problem with the items showing up painfully slowly...
                    // recommended by Mark Green in this thread:
                    // https://stackoverflow.com/questions/19510723/why-ui-is-freezing-during-wpf-datagrid-binding
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        PerformDisposeSafeOperation(() =>
                        {
                            wrapper.ViewItem = viewItem;
                            wrapper.DimensionInfo = cvItemDimInfo;
                            wrapper.IsViewItemLoaded = true;
                        });

                    }), DispatcherPriority.Background);
                }
            }));
        }

        public void CancelLoadingOfViewItem(string clipboardItemId)
        {
            // if the task has not already started, do not start it.
            _cancellableTaskManager.CancelStartingOfScheduledTask(taskId: clipboardItemId);
        }


        protected override void Dispose(bool disposing)
        {
            _isDisposing = true;

            _viewModel.RequestCloseOfMainWindow -= _viewModel_RequestCloseOfMainWindow;
            _settingsService.SettingChanged -= SettingsService_SettingChanged;

            _uiExtensionGenerationSlidingTimer.Dispose();
            _hintPopupSlidingTimer.Dispose();

            if (MainListView != null)
            {
                MainListView.Dispose();
            }

            _viewModel.Dispose();
            _viewModel = null;

            base.Dispose(disposing);
        }

        ~MainViewClipboardContents()
        {
            Debug.WriteLine("MainViewClipboardContents unloaded");
        }

        protected override Panel GetUIExtensionContainer()
        {
            return uiExtensionContainer;
        }

        private void SetToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainListView.AreSelectedItemsCombinable)
            {
                SetSelectedItemsToClipboard();
            }
        }

        private void TrashBtn_Click(object sender, RoutedEventArgs e)
        {
            deleteConfirmationPopup.IsOpen = true;
            this.IsEnabled = false;
        }

        private void DeleteConfirmationPopup_DialogAnswered(object sender, Dialog.EventArgsClasses.DialogAnswerEventArgs e)
        {
            if (e.SelectedButton == Dialog.Enums.DialogButtons.Yes)
            {
                IReadOnlyCollection<string> selectedItemIds = MainListView.GetSelectedItemIds();

                //MainListViewContainer.Visibility = Visibility.Hidden;

                // disable the post-selection logic and the compatibility checks, 
                // as the items in the item collection will change during deletion,
                // and compatibility checks normally will be requested.
                MainListView.SuppressCustomSelectionProcessing = true;

                LoadingPopupGovernor.OpenPopup(this, Brushes.White);

                Task.Run(() =>
                {
                    try
                    {
                        PerformDisposeSafeOperation
                        (() =>
                        {
                            IEnumerable<ClipboardViewItemWrapper> wrappers = _viewModel
                                    .ClipboardViewItemWrappers
                                    .Where(w => selectedItemIds.Contains(w.Id))
                                    .Where(w => w.IsViewItemLoaded);

                            bool isTherePinnedOrProtectedItems = wrappers.Any(w => w.ViewItem.IsPinned || w.ViewItem.IsProtected);

                            wrappers = wrappers
                                .Where(w => !w.ViewItem.IsPinned && !w.ViewItem.IsProtected);

                            List<string> idsToDelete = wrappers
                                .Select(w => w.Id)
                                .ToList();

                            _viewModel.DeleteItems(idsToDelete);

                            Dispatcher.Invoke(() =>
                            {
                                // decrease the total number of items to load in all pages
                                // by the number of the deleted items.
                                AllPagesItemsCount -= idsToDelete.Count();
                            });

                            if (isTherePinnedOrProtectedItems)
                            {
                               

                                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                                {
                                    // open the info popup
                                    deleteGroupPinnedItemsNotificationPopup.PlacementTarget = this;
                                    deleteGroupPinnedItemsNotificationPopup.Placement = PlacementMode.Center;
                                    deleteGroupPinnedItemsNotificationPopup.IsOpen = true;
                                }));
                            }
                        });

                        
                    }
                    finally
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                                // re-enable the post-selection logic and the compatibility checks
                                MainListView.SuppressCustomSelectionProcessing = false;
                                //MainListViewContainer.Visibility = Visibility.Visible;
                                this.IsEnabled = true;
                            LoadingPopupGovernor.ClosePopup();
                        }));
                    }
                });
            }
            else
            {
                this.IsEnabled = true;
            }
        }


        private void SetSelectedItemsToClipboard()
        {
            if (!MainListView.AreSelectedItemsCombinable)
            {
                throw new InvalidOperationException("The selected items are not combinable. The possibility of calling this method with such items should have been ruled out.");
            }

            List<ClipboardViewItemWrapper> wrappers = new List<ClipboardViewItemWrapper>();
            var selectedSubItemIds = MainListView.GetSelectedMultiItemsIds();
            if (selectedSubItemIds.Any())
            {
                wrappers.AddRange(MainListView.Items
                    .Cast<ClipboardViewItemWrapper>()
                    .Where(w => w.Id == selectedSubItemIds.Last().Key));
            }

            if (MainListView.SelectedItems.Count > 0)
            {
                wrappers.AddRange(MainListView.SelectedItems
                        .Cast<ClipboardViewItemWrapper>());
            }

            //TODO: notify the user and close the window
            foreach (ClipboardViewItemWrapper wrapper in wrappers)
            {
                if (!(MainListView.ItemContainerGenerator.ContainerFromItem(wrapper) is ListViewItem selectedListViewItem))
                {
                    throw new InvalidOperationException($"The list view does not contain an item with id {wrapper.Id}.");
                }

                var clipboardViewItemTile = Traversing.GetVisualChildOfType<ItemPresenterBase>(selectedListViewItem);
                if (!(clipboardViewItemTile is IAnimatableItem animatableItem))
                {
                    throw new InvalidOperationException($"The listviewitem for {wrapper.Id} does not implement {nameof(IAnimatableItem)}");
                }

                animatableItem.RunNotificationAnimation(AnimationPurpose.Success);
            }

            int delayMs = TileAnimationHelper.COLOR_ANIMATION_DURATION_MS + 400;
            // allow the user to see the animation before closing the window/
            Task.Delay(delayMs) // the duration of the color animation in the listview (FullClipboardListView) plus some extra time
                .ContinueWith((t) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                {
                    // raise the event...
                    SetItemsToClipboardRequested?.Invoke(this,
                                        new SetToClipboardEventArgs(MainListView.GetSelectedItemIds(), MainListView.GetSelectedMultiItemsIds(), keepGeneratedItemsCb.IsChecked == true));


                    CloseMainWindowRequested?.Invoke(this, new EventArgs());
                }));
            },
            TaskScheduler.FromCurrentSynchronizationContext());

        }

        private void MainListView_ClipboardItemContentControl_MouseDoubleClickDetection_DoubleClickDetected(object sender, EventArgs e)
        {
            if ((MainListView.AreAnyItemsSelected || MainListView.AreAnySubItemsSelected)
                        && MainListView.AreSelectedItemsCombinable)
            {
                SetSelectedItemsToClipboard();
            }
            else
            {
                DoSingleClickOperations();
            }
        }

        private void MainListView_ClipboardItemContentControl_MouseDoubleClickDetection_SingleClickDetected(object sender, EventArgs e)
        {
            DoSingleClickOperations();
        }

        private void DoSingleClickOperations()
        {
            IsLoadingOfUIExtensionsEnabled = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    IsShowingOfPostSelectionToolboxEnabled = true;
                    base.ClearLoadedUIExtensions();
                    if ((MainListView.AreAnyItemsSelected || MainListView.AreAnySubItemsSelected)
                        && MainListView.AreSelectedItemsCombinable)
                    {
                        base.LoadUIExtensions(MainListView);
                    }
                }
            ));
        }

        private void MainListView_ClipboardItemContentControl_MouseDoubleClickDetectionStarted(object sender, EventArgs e)
        {
            IsShowingOfPostSelectionToolboxEnabled = false;
            IsLoadingOfUIExtensionsEnabled = false;
            base.ClearLoadedUIExtensions();
        }

        private void MainListView_ClipboardItemsPanel_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                if (MainListView.AreSelectedItemsCombinable)
                {
                    SetSelectedItemsToClipboard();
                }
            }
        }

        private void MainListView_ViewItemLoadingRequested(object sender, EventArgsClasses.ClipboardItemManipulationEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                ScheduleLoadingOfViewItem(e.ClipboardItemId);
            }));
        }

        private void MainListView_ViewItemLoadingCancelRequested(object sender, EventArgsClasses.ClipboardItemManipulationEventArgs e)
        {
            CancelLoadingOfViewItem(e.ClipboardItemId);
        }

        private void MainListView_ViewItemUnloadingRequested(object sender, EventArgsClasses.ClipboardItemManipulationEventArgs e)
        {
            // unload the item to save up memory
            _viewModel.UnloadViewItem(e.ClipboardItemId);
        }

        private void MainListView_CombinabilityCheckRequested(object sender, CombinabilityEventArgs e)
        {
            base.RaiseCombinabilityCheckRequested(e);
        }

        private void MainListView_ItemEnableDisableSyncRequested(object sender, EventArgsClasses.ClipboardItemManipulationEventArgs e)
        {
            _viewModel.EnbleDisableSyncOfClipboardEntry(e.ClipboardItemId);
        }

        private void MainListView_ItemProtectUnprotectRequested(object sender, EventArgsClasses.ClipboardItemManipulationEventArgs e)
        {
            _viewModel.ProtectUnprotectClipboardEntry(e.ClipboardItemId);
        }

        public DateTime GroupDateUndergoingDeletion
        {
            get { return (DateTime)GetValue(GroupDateUndergoingDeletionProperty); }
            set { SetValue(GroupDateUndergoingDeletionProperty, value); }
        }

        public static readonly DependencyProperty GroupDateUndergoingDeletionProperty =
            DependencyProperty.Register(nameof(GroupDateUndergoingDeletion), typeof(DateTime), typeof(MainViewClipboardContents), new PropertyMetadata(default(DateTime)));



        private void MainListView_ItemGroupDeletionRequested(object sender, EventArgsClasses.ClipboardItemGroupManipulationEventArgs e)
        {
            // find the selected list view item by the ClipboardItemId
            var wrapper = _viewModel.ClipboardViewItemWrappers
                .OrderByDescending(w => w.DateCreated)
                .FirstOrDefault(w => w.DateCreated.Date == e.Day);

            if (wrapper == null)
            {
                throw new ArgumentException($"{nameof(_viewModel.ClipboardViewItemWrappers)} does not contain an item from {e.Day}.");
            }

            if (!(MainListView.ItemContainerGenerator.ContainerFromItem(wrapper) is ListViewItem selectedListViewItem))
            {
                throw new ArgumentException($"The list view does not contain an item with id {wrapper.Id}.");
            }

            //GroupItem itemGroupItem = Traversing.FindParent<GroupItem>(selectedListViewItem);
            GroupDateUndergoingDeletion = wrapper.DateCreated.Date;

            // unselet all selected items, including the one that we are going to delete
            MainListView.UnselectAll();
            MainListView.UnselectMultiItemSubItems();

            // store the original border brush of the group
            //Brush oldBorderBrush = itemGroupItem.BorderBrush;

            // set the popup location to that of the selected listview item
            deleteGroupConfirmationPopup.PlacementTarget = selectedListViewItem;

            // try to place the popup at the bottom of the item
            deleteGroupConfirmationPopup.Placement = PlacementMode.Bottom;

            // open the popup
            deleteGroupConfirmationPopup.IsOpen = true;

            // we are going to wait for the answer of the popup
            AutoResetEvent dialogMre = new AutoResetEvent(false);

            // declare an event handler which can access the variables in
            // our stack frame and is such that we can unsubscribe it from the event later.
            EventHandler<DialogAnswerEventArgs> answerHandler = (s, args) =>
            {
                if (args.SelectedButton == Dialog.Enums.DialogButtons.Yes)
                {
                    LoadingPopupGovernor.OpenPopup(this, Brushes.White);
                    Task.Run(() =>
                    {
                        try
                        {
                            PerformDisposeSafeOperation
                            (() =>
                            {
                                IEnumerable<ClipboardViewItemWrapper> wrappers = _viewModel
                                    .ClipboardViewItemWrappers
                                    .Where(w => w.DateCreated.Date == e.Day)
                                    .Where(w => w.IsViewItemLoaded);

                                bool isTherePinnedOrProtectedItems = wrappers.Any(w => w.ViewItem.IsPinned || w.ViewItem.IsProtected);

                                wrappers = wrappers
                                    .Where(w => !w.ViewItem.IsPinned && !w.ViewItem.IsProtected);

                                List<string> idsToDelete = wrappers
                                    .Select(w => w.Id)
                                    .ToList();

                                _viewModel.DeleteItems(idsToDelete);

                                // decrease the total number of items to load in all pages by the count of the deleted items
                                Dispatcher.Invoke(() =>
                                {
                                    AllPagesItemsCount -= idsToDelete.Count;
                                });

                                if (isTherePinnedOrProtectedItems)
                                {
                                    Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                                    {
                                        // open the info popup
                                        deleteGroupPinnedItemsNotificationPopup.PlacementTarget = this;
                                        deleteGroupPinnedItemsNotificationPopup.Placement = PlacementMode.Center;
                                        deleteGroupPinnedItemsNotificationPopup.IsOpen = true;
                                    }));
                                }
                            });
                        }
                        finally
                        {
                            try { LoadingPopupGovernor.ClosePopup(); }
                            finally
                            {
                                dialogMre.Set();
                            }
                        }
                    });
                }
                else
                {
                    dialogMre.Set();
                }
            };

            deleteGroupConfirmationPopup.DialogAnswered += answerHandler;

            // declare an event handler that will set the reset event upon closing the view,
            // so that we can still execute the cleanup code if the user closes the view
            // while the popup is still open.
            DependencyPropertyChangedEventHandler visHandler = (s, args) => dialogMre.Set();

            this.IsVisibleChanged += visHandler;

            // wait for the reset event in another task, so we do not block the ui thread 
            Task.Run(() =>
            {
                dialogMre.WaitOne();
                dialogMre.Dispose();

                // unsubscribe from the event handlers
                deleteGroupConfirmationPopup.DialogAnswered -= answerHandler;
                this.IsVisibleChanged -= visHandler;
            });
        }

        /// <summary>
        /// This method contains some really questionable code, but these days I am out of shape.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainListView_ItemDeletionRequested(object sender, EventArgsClasses.ClipboardItemManipulationEventArgs e)
        {
            // find the selected list view item by the ClipboardItemId
            var wrapper = _viewModel.ClipboardViewItemWrappers.FirstOrDefault(w => w.Id == e.ClipboardItemId);
            if (wrapper == null)
            {
                throw new ArgumentException($"{nameof(_viewModel.ClipboardViewItemWrappers)} does not contain an item with id {e.ClipboardItemId}.");
            }

            if (!(MainListView.ItemContainerGenerator.ContainerFromItem(wrapper) is ListViewItem selectedListViewItem))
            {
                throw new ArgumentException($"The list view does not contain an item with id {e.ClipboardItemId}.");
            }

            // unselet all selected items, including the one that we are going to delete
            MainListView.UnselectAll();
            MainListView.UnselectMultiItemSubItems();

            // disable the entire control
            this.IsEnabled = false;

            // store the original border brush of the item
            Brush oldBorderBrush = selectedListViewItem.BorderBrush;

            // color the border with the alert brush
            selectedListViewItem.BorderBrush = TryFindResource(ThemeBrush.AlertBrush1.ToString()) as Brush
                ?? Brushes.Red;

            // set the popup location to that of the selected listview item
            deleteSingleItemConfirmationPopup.PlacementTarget = selectedListViewItem;

            // try to place the popup at the bottom of the item
            deleteSingleItemConfirmationPopup.Placement = PlacementMode.Bottom;

            // open the popup
            deleteSingleItemConfirmationPopup.IsOpen = true;

            // we are going to wait for the answer of the popup
            AutoResetEvent dialogMre = new AutoResetEvent(false);

            // declare an event handler which can access the variables in
            // our stack frame and is such that we can unsubscribe it from the event later.
            EventHandler<DialogAnswerEventArgs> answerHandler = (s, args) =>
            {
                dialogMre.Set();
                if (args.SelectedButton == Dialog.Enums.DialogButtons.Yes)
                {
                    _viewModel.DeleteClipboardEntry(e.ClipboardItemId);

                    // decrease the total number of items to load in all pages by 1
                    AllPagesItemsCount -= 1;
                }
                else
                {
                    // if the user clicks No, the listview item will remain in the
                    // view, so we need to make it look as before - with its previous border brush.
                    Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
                        new Action(() => selectedListViewItem.BorderBrush = oldBorderBrush));
                }
            };

            deleteSingleItemConfirmationPopup.DialogAnswered += answerHandler;

            // declare an event handler that will set the reset event upon closing the view,
            // so that we can still execute the cleanup code if the user closes the view
            // while the popup is still open.
            DependencyPropertyChangedEventHandler visHandler = (s, args) => dialogMre.Set();

            this.IsVisibleChanged += visHandler;

            // wait for the reset event in another task, so we do not block the ui thread 
            Task.Run(() =>
            {
                dialogMre.WaitOne();
                dialogMre.Dispose();

                // unsubscribe from the event handlers
                deleteSingleItemConfirmationPopup.DialogAnswered -= answerHandler;
                this.IsVisibleChanged -= visHandler;

                // if the view is still visible (if it is not being closed and we
                // are not coming here from the event handler of IsVisibleChanged),
                // then re-enable the entire control.
                if (this.IsVisible)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
                        new Action(() => this.IsEnabled = true));
                }
            });
        }

        private void MainListView_SelectionChangeCompleted(object sender, SelectionEventArgs e)
        {
            // SelectionChangeCompleted fires after the combinability check has finished,
            // so it is completely safe to check the compatibility property here.
            if ((MainListView.AreAnyItemsSelected || MainListView.AreAnySubItemsSelected)
                && MainListView.AreSelectedItemsCombinable)
            {
                LoadUIExtensionsWithDelay();
                ShowHintPopupWithDelay();
            }
            else
            {
                base.ClearLoadedUIExtensions();
            }
        }

        /// <summary>
        /// This property resets its state after the ui extension loading has been attempted.
        /// So if it is set to false, upon the attempt to load the ui extensions no extension will be
        /// loaded and the property will be set to true.
        /// </summary>
        protected bool IsLoadingOfUIExtensionsEnabled { get; set; }

        private void LoadUIExtensionsWithDelay()
        {
            base.ClearLoadedUIExtensions();

            if (MainListView.SelectedItems.Count > 0)
            {
                _uiExtensionGenerationSlidingTimer.Slide();
            }
        }

        private void _uiExtensionGenerationSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
            (Action)(() =>
            {
                PerformDisposeSafeOperation(() =>
                {
                    if (IsLoadingOfUIExtensionsEnabled)
                    {
                        base.LoadUIExtensions(MainListView);
                    }
                    else
                    {
                        // re-enable the loading of the extensions
                        IsLoadingOfUIExtensionsEnabled = true;
                    }
                });
            }));

        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            bool setAsSeen = _lastShownHintPopupName == PopupHintNames.PP_HINT_CHARGE_CLIPBOARD
                && setToClipboardBtn.IsMouseOver;
            //|| (_lastShownHintPopupName == some other name && other buttin is mouse over)


            _popupHintService.HideHintPopup(setAsSeen);
        }

        private void ShowHintPopupWithDelay()
        {
            _popupHintService.HideHintPopup();
            _hintPopupSlidingTimer.Slide();
        }

        private void MainListView_GroupingComplete(object sender, EventArgs e)
        {
            LoadingPopupGovernor.ClosePopup();

            // if we do not set the visibility in background, 
            // the fade animations controlled by the triggers do not work
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        if (SideBarGrid.Visibility != Visibility.Visible)
                        {
                            SideBarGrid.Visibility = Visibility.Visible;
                        }

                        if (TopBarGrid.Visibility != Visibility.Visible)
                        {
                            TopBarGrid.Visibility = Visibility.Visible;
                        }

                        MainListViewContainer.Visibility = Visibility.Visible;
                        // TODO: the child should become visible when the container becomes visible... In this case it does not. Figure out why.
                        MainListView.Visibility = Visibility.Visible;

                        // focus the mainlistview only if the contents changed due to things other than search
                        if (!(Keyboard.FocusedElement is TextBox))
                        {
                            MainListView.Focus();
                        }
                    }));
        }

        private void ExpandableSearchBox_SearchRequested(object sender, EventArgsClasses.SearchRequestEventArgs e)
        {
            UpdateListViewWithSearchTerms();
        }

        private void FilterBand_SearchTermsChanged(object sender, EventArgs e)
        {
            UpdateListViewWithSearchTerms();
        }

        private void UpdateListViewWithSearchTerms()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                MainListViewContainer.Visibility = Visibility.Hidden;

                _searchTerms.Clear();

                if (!string.IsNullOrWhiteSpace(searchTextBox.SearchText))
                {
                    _searchTerms.Add
                    (
                        new TextSearchTerm(new List<string> { searchTextBox.SearchText })
                    );
                }

                _searchTerms.AddRange(filterBand.SearchTerms);

                LoadContents(0, _maxNumOfItemsToLoadAtOnce, clearOldContents: true);
            }));
        }

        /// <summary>
        /// This is not really safe as it does not use locking, but it will do for now...
        /// I am afraid to implement locking in the Dispose method so I will take the risk
        /// of leaving corrupt data after some exceptions from the disposed viewmodel's methods
        /// and will implement simple checking of a volatile flat and try/catch instead.
        /// </summary>
        /// <param name="action"></param>
        private void PerformDisposeSafeOperation(Action action)
        {
            if (_isDisposing)
            {
                return;
            }

            try
            {
                action();
            }
            catch
            {
                if (!_isDisposing)
                {
                    throw;
                }
            }
        }


        private void MainListView_LoadMoreItemsRequested(object sender, EventArgs e)
        {
            MainListViewContainer.Visibility = Visibility.Hidden;

            LoadContents(MainListView.Items.Count, _maxNumOfItemsToLoadAtOnce);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindowOpenRequested?.Invoke(this, new SettingsWindowEventArgs(SettingsCategory.General));
        }

        private void CloudErrorButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindowOpenRequested?.Invoke(this, new SettingsWindowEventArgs(SettingsCategory.Cloud));
        }

        private void RebuildIndexButton_Click(object sender, RoutedEventArgs e)
        {
            RebuildIndexRequested?.Invoke(this, new EventArgs());
        }

        private void DeleteGroupPinnedItemsNotificationPopup_DialogAnswered(object sender, DialogAnswerEventArgs e)
        {
            // unselect the pinned or protected items that were not deleted, but are still selected
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => MainListView.UnselectAll()));
        }

        private void UpdateAvailableButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                _updateNotificationService.LaunchUpdate();
            });

            CloseMainWindowRequested?.Invoke(this, new EventArgs());
        }

        private void BtnCloseMainWindow_Click(object sender, RoutedEventArgs e)
        {
            CloseMainWindowRequested?.Invoke(this, new EventArgs());
        }
    }
}
