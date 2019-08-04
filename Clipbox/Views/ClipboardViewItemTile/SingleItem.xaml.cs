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
    /// Interaction logic for SingleItem.xaml
    /// </summary>
    public partial class SingleItem : UserControl
    {
        public SingleItem()
        {
            InitializeComponent();

            mainGrid.DataContext = this;
        }



        public ClipboardViewItemSingle ClipboardViewItem
        {
            get { return (ClipboardViewItemSingle)GetValue(ClipboardViewItemProperty); }
            set { SetValue(ClipboardViewItemProperty, value); }
        }

        public static readonly DependencyProperty ClipboardViewItemProperty =
            DependencyProperty.Register("ClipboardViewItem", typeof(ClipboardViewItemSingle), typeof(SingleItem), new PropertyMetadata(null, ClipboardViewItem_ValueChanged));

        private static void ClipboardViewItem_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
        }

        public bool IsDimmed
        {
            get { return (bool)GetValue(IsDimmedProperty); }
            set { SetValue(IsDimmedProperty, value); }
        }

        public static readonly DependencyProperty IsDimmedProperty =
            DependencyProperty.Register("IsDimmed", typeof(bool), typeof(SingleItem), new PropertyMetadata(false));




        public double ThumbnailAreaMaxHeight
        {
            get { return (double)GetValue(ThumbnailAreaMaxHeightProperty); }
            set { SetValue(ThumbnailAreaMaxHeightProperty, value); }
        }
        
        public static readonly DependencyProperty ThumbnailAreaMaxHeightProperty =
            DependencyProperty.Register("ThumbnailAreaMaxHeight", typeof(double), typeof(SingleItem), new PropertyMetadata(0d));




    }
}
