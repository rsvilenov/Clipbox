using Clipbox.Infrastructure.UI.Utils;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.Enums.ClipboardViewItemTile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipbox.Views.ClipboardViewItemTile
{
    /// <summary>
    /// Interaction logic for GroupItem.xaml
    /// </summary>
    public partial class MultiItem : MultiItemBase, IAnimatableItem
    {
        public static readonly RoutedEvent SubItems_SelectionChangedEvent
        = EventManager.RegisterRoutedEvent(
            "SubItems_SelectionChanged",
            RoutingStrategy.Bubble,
            typeof(SelectionChangedEventHandler),
            typeof(MultiItem));

        public event SelectionChangedEventHandler SubItems_SelectionChanged
        {
            add { AddHandler(SubItems_SelectionChangedEvent, value); }
            remove { RemoveHandler(SubItems_SelectionChangedEvent, value); }
        }

        public static readonly RoutedEvent SubItems_SubItemsFocusMovedOutEvent
        = EventManager.RegisterRoutedEvent(
            "SubItems_SubItemsFocusMovedOut",
            RoutingStrategy.Bubble,
            typeof(EventHandler<RoutedEventArgs>),
            typeof(MultiItem));

        public event EventHandler<RoutedEventArgs> SubItems_SubItemsFocusMovedOut
        {
            add { AddHandler(SubItems_SubItemsFocusMovedOutEvent, value); }
            remove { RemoveHandler(SubItems_SubItemsFocusMovedOutEvent, value); }
        }

        public MultiItem()
        {
            InitializeComponent();
            
            mainGrid.DataContext = this;

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    if (ClipboardViewItem?.TotalClipboardItemsCount != null)
                    {
                        NumberOfUniformGridColumns = ClipboardViewItem.TotalClipboardItemsCount <= 4 ? 2 : 3;
                    }
                }));
        }

        /// <summary>
        /// Retrieves a reference to the internal list view.
        /// </summary>
        /// <returns></returns>
        public ListView GetSubItemsListView()
        {
            return subItemsListView;
        }

        public ClipboardViewItemGroup ClipboardViewItem
        {
            get { return (ClipboardViewItemGroup)GetValue(ClipboardViewItemProperty); }
            set { SetValue(ClipboardViewItemProperty, value); }
        }

        public static readonly DependencyProperty ClipboardViewItemProperty =
            DependencyProperty.Register("ClipboardViewItem", typeof(ClipboardViewItemGroup), typeof(MultiItem), new PropertyMetadata(null));

        
        public bool IsDimmed
        {
            get { return (bool)GetValue(IsDimmedProperty); }
            set { SetValue(IsDimmedProperty, value); }
        }

        public static readonly DependencyProperty IsDimmedProperty =
            DependencyProperty.Register("IsDimmed", typeof(bool), typeof(MultiItem), new PropertyMetadata(false));

        private void SubItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RaiseEvent(new SelectionChangedEventArgs(SubItems_SelectionChangedEvent, e.AddedItems, e.RemovedItems));
        }

        private void ListViewScrollTransferFocusBehavior_KeyboardFocusMovedOut(object sender, EventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(SubItems_SubItemsFocusMovedOutEvent));
        }

        public void RunNotificationAnimation(AnimationPurpose animationPurpose)
        {
            if (subItemsListView.SelectedItems.Count > 0)
            {
                foreach (var selectedItem in subItemsListView.SelectedItems)
                {
                    ListViewItem selectedListViewItem = (ListViewItem)subItemsListView.ItemContainerGenerator.ContainerFromItem(selectedItem);
                    if (!(selectedListViewItem.Template.FindName("Bd", selectedListViewItem) is Border border))
                    {
                        throw new InvalidOperationException("Cannot find an item named Bd in the selected subitem's content template.");
                    }

                    TileAnimationHelper.RunAnimation(border, animationPurpose);
                }
            }
            else
            {
                TileAnimationHelper.RunAnimation(borderToAnimate, animationPurpose);
            }
        }
    }
}
