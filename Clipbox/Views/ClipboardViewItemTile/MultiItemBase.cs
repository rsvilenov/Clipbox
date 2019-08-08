using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views.ClipboardViewItemTile
{
    public class MultiItemBase : UserControl
    {
        protected int NumberOfUniformGridColumns
        {
            get { return (int)GetValue(NumberOfUniformGridColumnsProperty); }
            set { SetValue(NumberOfUniformGridColumnsProperty, value); }
        }

        protected static readonly DependencyProperty NumberOfUniformGridColumnsProperty =
            DependencyProperty.Register(nameof(NumberOfUniformGridColumns), typeof(int), typeof(MultiItem), new PropertyMetadata(0));
        
    }
}
