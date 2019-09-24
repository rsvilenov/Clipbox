using System.Windows.Controls;
using System.Windows.Interactivity;

namespace Clipbox.Behaviors
{
    /// <summary>
    /// When the ListView receives focus, set the focus to the last selected item
    /// or the first item in the list if no item is selected.
    /// </summary>
    public class ListViewOnFocusSelectFirstItemBehavior : Behavior<ListView>
    {
        protected override void OnAttached()
        {
            AssociatedObject.GotFocus += AssociatedObject_GotFocus;
        }

        private void AssociatedObject_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            // prevent recursion - when the ListViewItem receives the focus, the ListView's GotFocus event fires as well.
            if (e.OriginalSource?.GetType() != typeof(ListViewItem))
            {
                // focus the last selected item or the first item in the list if none is selected
                var item = (ListViewItem)AssociatedObject.ItemContainerGenerator.ContainerFromIndex(AssociatedObject.SelectedIndex == -1 ? 0 : AssociatedObject.SelectedIndex);
                if (item != null)
                {
                    item.Focus();
                }
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.GotFocus -= AssociatedObject_GotFocus;
        }
    }
}
