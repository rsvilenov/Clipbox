using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views.ClipboardViewItemTile
{
    public class MultiItemBase : ItemBase
    {
        protected int NumberOfUniformGridColumns
        {
            get { return (int)GetValue(NumberOfUniformGridColumnsProperty); }
            set { SetValue(NumberOfUniformGridColumnsProperty, value); }
        }

        protected static readonly DependencyProperty NumberOfUniformGridColumnsProperty =
            DependencyProperty.Register(nameof(NumberOfUniformGridColumns), typeof(int), typeof(MultiItemBase), new PropertyMetadata(0));
        
    }
}
