using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Services.EventArgsClasses;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.ClipboardViewItemTile;
using Clipbox.Views.EventArgsClasses;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Clipbox.Views.ClipboardListView
{
    public abstract class ClipboardListViewBase : ListView
    {
        private string _lastSelectedItemId;

        private bool _areLastSelectedItemsCombinable;

        private List<MultiItem> _loadedMultiItems;

        private int _hashCodeOfItemUndergoingDeletion;
        
        protected List<string> SelectedItemIds { get; set; }

        protected Dictionary<string, List<int>> SelectedMultiItemsIds { get; set; }

        public event EventHandler<SelectionEventArgs> SelectionChangeCompleted;

        public event EventHandler<CombinabilityEventArgs> CombinabilityCheckRequested;

        public event EventHandler<ClipboardItemManipulationEventArgs> ItemDeletionRequested;

        public event EventHandler<ClipboardItemManipulationEventArgs> ItemPinUnpinRequested;

        /// <summary>
        /// Contains the command for the Close and Pin buttons.
        /// We do not want these commands to be publicly accessible, but we cannot make the 
        /// ICommand properties protected as they should be public or they will not work, yet we do not want to 
        /// use a ViewModel for these user controls, as the logic in them is purely UI-related, so we are using this... 
        /// Unlike ICommand properties, Dependency properties can be private and protected and the binding will still work.
        /// </summary>
        protected ActionButtonsDataContextClass ActionButtonsDataContext
        {
            get { return (ActionButtonsDataContextClass)GetValue(ActionButtonsDataContextProperty); }
            set { SetValue(ActionButtonsDataContextProperty, value); }
        }

        protected static readonly DependencyProperty ActionButtonsDataContextProperty =
            DependencyProperty.Register(nameof(ActionButtonsDataContext), typeof(ActionButtonsDataContextClass), typeof(ClipboardListViewBase), new PropertyMetadata(null));


        public double ClipboardViewItemMaxHeight
        {
            get { return (double)GetValue(ClipboardViewItemHeightProperty); }
            set { SetValue(ClipboardViewItemHeightProperty, value); }
        }


        public static readonly DependencyProperty ClipboardViewItemHeightProperty =
            DependencyProperty.Register(nameof(ClipboardViewItemMaxHeight), typeof(double), typeof(ClipboardListViewBase), new PropertyMetadata(0d));
        
        private static readonly DependencyPropertyKey AreAnyItemsSelectedPropertyKey
        = DependencyProperty.RegisterReadOnly(nameof(AreAnyItemsSelected), typeof(bool), typeof(ClipboardListViewBase),
            new FrameworkPropertyMetadata(default(bool),
                FrameworkPropertyMetadataOptions.None));

        public static readonly DependencyProperty AreAnyItemsSelectedProperty
            = AreAnyItemsSelectedPropertyKey.DependencyProperty;

        /// <summary>
        /// A read only DP, indicating whether there are any selected items.
        /// </summary>
        public bool AreAnyItemsSelected
        {
            get { return (bool)GetValue(AreAnyItemsSelectedProperty); }
            protected set { SetValue(AreAnyItemsSelectedPropertyKey, value); }
        }


        private static readonly DependencyPropertyKey AreAnySubItemsSelectedPropertyKey
        = DependencyProperty.RegisterReadOnly(nameof(AreAnySubItemsSelected), typeof(bool), typeof(ClipboardListViewBase),
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

        public ClipboardListViewBase()
        {
            SelectedItemIds = new List<string>();
            SelectedMultiItemsIds = new Dictionary<string, List<int>>();
            _loadedMultiItems = new List<MultiItem>();

            Unloaded += ClipboardListViewBase_Unloaded;

            ActionButtonsDataContext = new ActionButtonsDataContextClass
            {
                DeleteClipboardEntry = new DelegateCommand<ClipboardViewItem>(DeleteClipboardEntryHandler,
                /* disable the command until all deletion operations execute (it should be just one, so do it just in case... 
                 * and prevent the possibility of the user to click on the button twice in a row) */
                (item) => item?.GetHashCode() != _hashCodeOfItemUndergoingDeletion),

                // wait for the binding operations to complete,
                // so we can get the changed value of the IsPinned property
                PinUnpinClipboardEntry = new DelegateCommand<ClipboardViewItem>(PinUnpinClipboardEntryHandler,
                dispatcherPriority: DispatcherPriority.ContextIdle)
            };
        }

        private void ClipboardListViewBase_Unloaded(object sender, RoutedEventArgs e)
        {
            // if we do not set this to null, a memory leak occurs - the control stays alive forever,
            // keeping the entire view and the window alive...
            // TODO: figure out why
            ActionButtonsDataContext = null;

            foreach (var multiItem in _loadedMultiItems)
            {
                multiItem.SubItems_SelectionChanged -= MultiItem_SubItems_SelectionChanged;
                multiItem.SubItems_SubItemsFocusMovedOut -= MultiItem_SubItems_SubItemsFocusMovedOut;
                multiItem.Unloaded -= MultiItem_Unloaded;
                multiItem.Loaded -= MultiItem_Loaded;
            }

            _loadedMultiItems.Clear();
        }

        private void PinUnpinClipboardEntryHandler(ClipboardViewItem parameter)
        {
            ItemPinUnpinRequested?.Invoke(this, new ClipboardItemManipulationEventArgs(parameter.ClipboardItemId));
        }

        private void DeleteClipboardEntryHandler(ClipboardViewItem parameter)
        {
            // block consequent executions of the command until the entire delete operation is executed
            _hashCodeOfItemUndergoingDeletion = parameter.GetHashCode();
            ItemDeletionRequested?.Invoke(this, new ClipboardItemManipulationEventArgs(parameter.ClipboardItemId));
        }

        public IReadOnlyCollection<string> GetSelectedItemIds()
        {
            return SelectedItemIds
                .ToList() // clone the collection
                .AsReadOnly();
        }

        
        public IReadOnlyDictionary<string, IReadOnlyCollection<int>> GetSelectedMultiItemsIds()
        {
            return new ReadOnlyDictionary<string, IReadOnlyCollection<int>>
                (SelectedMultiItemsIds.ToDictionary(k => k.Key, v => (IReadOnlyCollection<int>)v.Value.ToList().AsReadOnly()));
        }
        

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
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

        public void UnselectMultiItemSubItems()
        {
            foreach (MultiItem multiItem in _loadedMultiItems)
            {
                multiItem.GetSubItemsListView()?.UnselectAll();
            }
        }


        private void UpdateAreAnyItemsSelected()
        {
            AreAnyItemsSelected = SelectedItemIds.Any() || SelectedMultiItemsIds.Any();
        }

        private void UpdateAreAnySubItemsSelected()
        {
            AreAnySubItemsSelected = SelectedMultiItemsIds.Any();
        }

        /// <summary>
        /// MultiItem Loaded event. Bind this through an EventSetter in the XAML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void MultiItem_Loaded(object sender, RoutedEventArgs e)
        {
            var multiItem = (MultiItem)sender;
            multiItem.SubItems_SelectionChanged += MultiItem_SubItems_SelectionChanged;
            multiItem.SubItems_SubItemsFocusMovedOut += MultiItem_SubItems_SubItemsFocusMovedOut;
            _loadedMultiItems.Add(multiItem);
        }

        /// <summary>
        /// MultiItem Unloaded event. Bind this through an EventSetter in the XAML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void MultiItem_Unloaded(object sender, RoutedEventArgs e)
        {
            MultiItem multiItem = (MultiItem)sender;

            multiItem.SubItems_SelectionChanged -= MultiItem_SubItems_SelectionChanged;
            multiItem.SubItems_SubItemsFocusMovedOut -= MultiItem_SubItems_SubItemsFocusMovedOut;
            multiItem.Unloaded -= MultiItem_Unloaded;
            multiItem.Loaded -= MultiItem_Loaded;

            _loadedMultiItems.Remove(multiItem);
        }

        private void MultiItem_SubItems_SubItemsFocusMovedOut(object sender, RoutedEventArgs e)
        {
            MultiItem multiItem = (MultiItem)sender;

            Traversing.FindParent<ListViewItem>(multiItem).Focus();
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

            ClipboardViewItemWrapper mainViewItemWrapper = this.Items.Cast<ClipboardViewItemWrapper>().
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
                    this.SelectedItems.Remove(mainViewItemWrapper);
                    mainViewItemWrapper.IsSelected = false;
                }

                SelectedMultiItemsIds.Add(mainViewItem.ClipboardItemId, selectedIndices.ToList());
            }

            IEnumerable<ClipboardViewItemWrapper> mainListViewSelectedItems = this.SelectedItems.Cast<ClipboardViewItemWrapper>();


            //// Due to the fact that we raise the selection and click events programmatically, we are limited to using MainListView_SelectionChanged.
            //// MainListView.Items.CurrentChanging would have been of great help in cancelling the selection, but unfortunately we cannot use it here.

            bool areSelectedItemsCombinable = true;

            var combinabilityEventArgs = new CombinabilityEventArgs(mainListViewSelectedItems.Select(item => item.Id).ToList(), SelectedMultiItemsIds);
            
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
                this.UnselectAll();
            }

            UpdateAreAnyItemsSelected();
            UpdateAreAnySubItemsSelected();

            SelectionChangeCompleted?.Invoke(this, combinabilityEventArgs);
        }

        protected void PerformOnSelectedChangedOpreations()
        {
            Debug.WriteLine("SelectionChanged");

            if (_lastSelectedItemId == null)
            {
                return;
            }

            IEnumerable<ClipboardViewItemWrapper> selectedItems = this.SelectedItems.Cast<ClipboardViewItemWrapper>();

            // Due to the fact that we raise the selection and click events programmatically, we are limited to using MainListView_SelectionChanged.
            // MainListView.Items.CurrentChanging would have been of great help in cancelling the selection, but unfortunately we cannot use it here.
            var allSelectedItemIds = selectedItems.Select(item => item.Id).Union(SelectedMultiItemsIds.Keys);

            var combinabilityEventArgs = new CombinabilityEventArgs(selectedItems.Select(item => item.Id).ToList(), SelectedMultiItemsIds);
            
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
                    ClipboardViewItemWrapper latestSelectedItem = this
                        .Items
                        .Cast<ClipboardViewItemWrapper>()
                        .Single(item => item.Id == _lastSelectedItemId);

                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, (Action)(() =>
                    {
                        if (latestSelectedItem != null)
                        {
                            this.SelectedItems.Remove(latestSelectedItem);

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

            foreach (ClipboardViewItemWrapper item in this
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

            SelectionChangeCompleted?.Invoke(this, combinabilityEventArgs);

#if DEBUG
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif

        }

        protected virtual void OnSelectedItemsCombinabilityChanged(SelectedItemsCombinabilityArgs args)
        { }
        
        public virtual void ResumeDefaultState()
        { }

        protected class SelectedItemsCombinabilityArgs
        {
            public SelectedItemsCombinabilityArgs(bool areItemsCombinable)
            {
                AreItemsCombinable = areItemsCombinable;
            }

            public bool AreItemsCombinable { get; }

            public bool CancelLastSelection { get; set; }
        }

        protected class ActionButtonsDataContextClass
        {
            public ICommand DeleteClipboardEntry { get; set; }

            public ICommand PinUnpinClipboardEntry { get; set; }
        }
    }
}
