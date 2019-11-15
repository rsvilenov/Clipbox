using Clipbox.Core.Search;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Theme.Enums;
using Clipbox.Infrastructure.UI.Utils;
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
        private MainViewClipboardContentsViewModel _viewModel;
        
        public event EventHandler<SetToClipboardEventArgs> SetItemsToClipboardRequested;

        public event EventHandler CloseMainWindowRequested;



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

        
        private CancellableTaskManager _cancellableTaskManager;

        private SlidingTimer _uiExtentionGenerationSlidingTimer;

        /// <summary>
        /// This field provides not a nice solution to the problem of
        /// running dispatcher actions or tasks, requiring resources,
        /// destroyed by Dispose(), which run after the Dispose() has started...
        /// </summary>
        private volatile bool _isDisposing;

        public MainViewClipboardContents(MainViewClipboardContentsViewModel vm, 
            UIExtentionContainer container, 
            ISettingsService settingsService)
            : base (container, settingsService)
        {
            InitializeComponent();
            _viewModel = vm;
            DataContext = _viewModel;

            _cancellableTaskManager = new CancellableTaskManager();

            _uiExtentionGenerationSlidingTimer = new SlidingTimer
            {
                IntervalMs = LoadingOfUIExtensionsDelayMs
            };
            _uiExtentionGenerationSlidingTimer.Elapsed += _uiExtentionGenerationSlidingTimer_Elapsed;


            Loaded += MainViewClipboardContents_Loaded;
            IsVisibleChanged += MainViewClipboardContents_IsVisibleChanged;

            // this setting should not change while this view is open, so no SettingChanged subscription is needed.
            AreFadeAnimationsEnabled = settingsService.GetSetting<bool>(SettingsKeys.AreFadeAnimationsEnabled);
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
                LoadContents(null);
            }
        }

        private void LoadContents(IEnumerable<ISearchTerm> searchTerms)
        {
            if (!(SynchronizationContext.Current is DispatcherSynchronizationContext))
            {
                throw new InvalidOperationException("This method should be called from the dispatcher thread only");
            }

            _viewModel.ClipboardViewItemWrappers.EnableSyncForCurrentUIThread();
            IEnumerable<string> clipboardItemIds = null;
            IReadOnlyCollection<string> removedClipboardItemIds = null;

            Task.Run(() =>
            {
                PerformDisposeSafeOperation
                (
                    () => clipboardItemIds = _viewModel.GetClipboardItemIds(searchTerms) // maybe pass filters to this method later
                );

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
                            MainListViewContainer.Visibility = Visibility.Visible;
                            // we should also show the top and side bars, so that the
                            // user knows that what is shown on the screen is actually
                            // the application and not an empty desktop :)
                            TopBarGrid.Visibility = Visibility.Visible;
                            SideBarGrid.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            PerformDisposeSafeOperation
                            (
                                () => _viewModel.LoadAllItems(this.MonitorDimensions, itemsToLoad)
                            );
                        }

#if DEBUG
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
#endif
                    }));
            },
            // ensure that the code in ContinueWith executes on the main thread,
            // which should be the UI thread, so we have access to the correct dispatcher
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void ScheduleLoadingOfViewItem(string clipboardItemId)
        {

            ClipboardViewItemWrapper wrapper = null;
            ClipboardViewItemDimensionInfo cvItemDimInfo = null;
            ClipboardViewItem viewItem = null;

            // run the slow-running operations in a separate task
            _cancellableTaskManager.ScheduleTask(clipboardItemId, new Action<string>((taskId) =>
            {
                PerformDisposeSafeOperation(() =>
                {
                    wrapper = _viewModel.GetViewItemWrapper(clipboardItemId);
                    if (wrapper == null)
                    {
                        // by the time this task executes, the item may has been filtered out by the search or the filters...
                        return;
                    }

                    _viewModel.TryGetPreviewDimensions(base.MonitorDimensions, wrapper, out cvItemDimInfo);
                    
                    viewItem = _viewModel.GetViewItem(clipboardItemId, base.MonitorDimensions);
                });

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
            }));
        }

        public void CancelLoadingOfViewItem(string clipboardItemId)
        {
            _cancellableTaskManager.CancelScheduledTask(taskId: clipboardItemId);
        }
        

        protected override void Dispose(bool disposing)
        {
            _isDisposing = true;
            
            _uiExtentionGenerationSlidingTimer.Dispose();

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

        protected override Panel GetUIExtentionContainer()
        {
            return uiExtentionContainer;
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
            try
            {
                if (e.SelectedButton == Dialog.Enums.DialogButtons.Yes)
                {
                    IReadOnlyCollection<string> selectedItemIds = MainListView.GetSelectedItemIds();

                    MainListViewContainer.Visibility = Visibility.Hidden;
                    // disable the post-selection logic and the compatibility checks, 
                    // as the items in the item collection will change during deletion,
                    // and compatibility checks normally will be requested.
                    MainListView.SuppressCustomSelectionProcessing = true;

                    Task.Run(() =>
                    {
                        try
                        {
                            PerformDisposeSafeOperation
                            (
                                () => _viewModel.DeleteItems(selectedItemIds)
                            );
                        }
                        finally
                        {
                            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                // re-enable the post-selection logic and the compatibility checks
                                MainListView.SuppressCustomSelectionProcessing = false;
                                MainListViewContainer.Visibility = Visibility.Visible;
                            }));
                        }
                    });
                }
            }
            finally
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

                var clipboardViewItemTile = Traversing.GetVisualChildOfType<ItemBase>(selectedListViewItem);
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
            if (MainListView.AreSelectedItemsCombinable
                && MainListView.GetSelectedItemIds().Count == 1
                && MainListView.GetSelectedMultiItemsIds().Count == 0)
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
                    if ((MainListView.AreAnyItemsSelected || MainListView.AreAnyItemsSelected)
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
            ScheduleLoadingOfViewItem(e.ClipboardItemId);
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

        private void MainListView_ItemPinUnpinRequested(object sender, EventArgsClasses.ClipboardItemManipulationEventArgs e)
        {
            _viewModel.PinUnpinClipboardEntry(e.ClipboardItemId);
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
            if (MainListView.AreAnyItemsSelected || MainListView.AreAnyItemsSelected)
            {
                if (MainListView.AreSelectedItemsCombinable)
                {
                    LoadUIExtensionsWithDelay();
                }
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
                _uiExtentionGenerationSlidingTimer.Slide();
            }
        }

        private void _uiExtentionGenerationSlidingTimer_Elapsed(object sender, EventArgs e)
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

        private void MainListView_GroupingComplete(object sender, EventArgs e)
        {
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

                List<ISearchTerm> searchTerms = new List<ISearchTerm>();

                if (!string.IsNullOrWhiteSpace(searchTextBox.SearchText))
                {
                    searchTerms.Add
                    (
                        new TextSearchTerm(new List<string> { searchTextBox.SearchText })
                    );
                }

                searchTerms.AddRange(filterBand.SearchTerms);
                
                
                _viewModel.ClipboardViewItemWrappers.Clear();
                LoadContents(searchTerms);
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

        private void FilterBand_AdvancedSearchOpenRequested(object sender, EventArgs e)
        {
            advancedSearchPopup.IsOpen = true;
            this.IsEnabled = false;
        }
    }
}
