using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace Clipbox.Behaviors
{
    /// <summary>
    /// If a listviewitem is selected, a mouse click will unselect it.
    /// Attach this behavior to the container of the ListView's ItemTemplate.
    /// </summary>
    public class ListViewItemUnselectOnClickBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            AssociatedObject.MouseDown += AssociatedObject_MouseDown;
        }

        private void AssociatedObject_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!KeyboardUtils.IsKeyPressed(Win32.Native.NativeEnums.Keys.LeftControl)
                && !KeyboardUtils.IsKeyPressed(Win32.Native.NativeEnums.Keys.RightContol))
            {
                var listViewItem = Traversing.FindParent<ListViewItem>(AssociatedObject);
                if (listViewItem != null && listViewItem.IsSelected)
                {
                    listViewItem.IsSelected = false;
                    e.Handled = true;
                }
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseDown -= AssociatedObject_MouseDown;
        }
    }
}
