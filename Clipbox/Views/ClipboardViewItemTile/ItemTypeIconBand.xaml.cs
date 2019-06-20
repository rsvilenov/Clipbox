using Clipbox.Core.Enums;
using Clipbox.Infrastructure.UI;
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
    /// Interaction logic for ItemTypeIconBand.xaml
    /// </summary>
    public partial class ItemTypeIconBand : UserControl
    {
        public ItemTypeIconBand()
        {
            InitializeComponent();
            this.itemsControl.DataContext = this;
        }



        public bool IsGroupItem
        {
            get { return (bool)GetValue(IsGroupItemProperty); }
            set { SetValue(IsGroupItemProperty, value); }
        }
        
        public static readonly DependencyProperty IsGroupItemProperty =
            DependencyProperty.Register("IsGroupItem", typeof(bool), typeof(ItemTypeIconBand), new PropertyMetadata(false));
        
        public AsyncObservableCollection<ClipboardContentClass> ContentClasses
        {
            get { return (AsyncObservableCollection<ClipboardContentClass> )GetValue(ContentClassesProperty); }
            set { SetValue(ContentClassesProperty, value); }
        }

        public static readonly DependencyProperty ContentClassesProperty =
            DependencyProperty.Register("ContentClasses", typeof(AsyncObservableCollection<ClipboardContentClass> ), typeof(ItemTypeIconBand), new PropertyMetadata(new AsyncObservableCollection<ClipboardContentClass>()));


    }
}
