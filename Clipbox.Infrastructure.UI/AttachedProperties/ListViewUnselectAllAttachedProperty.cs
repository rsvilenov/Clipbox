using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Infrastructure.UI.AttachedProperties
{
    public static class ListViewUnselectAllAttachedProperty
    {
        public static bool GetUnselectAll(ListView obj)
        {
            return (bool)obj.GetValue(UnselectAllProperty);
        }

        public static void SetUnselectAll(ListView obj, bool value)
        {
            obj.SetValue(UnselectAllProperty, value);
        }
        
        public static readonly DependencyProperty UnselectAllProperty =
            DependencyProperty.RegisterAttached("UnselectAll", typeof(bool), typeof(ListViewUnselectAllAttachedProperty), new UIPropertyMetadata(false, UnselectAllPropertyChanged));

        private static void UnselectAllPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ((ListView)d).UnselectAll();
            }
        }
    }
}
