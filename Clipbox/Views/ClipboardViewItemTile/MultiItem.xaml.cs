using Clipbox.ViewModels.BindableObjects;
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
    public partial class MultiItem : UserControl
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

        public MultiItem()
        {
            InitializeComponent();
            
            mainGrid.DataContext = this;
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
    }
}
