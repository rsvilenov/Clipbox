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
    /// Interaction logic for SingleItemLoading.xaml
    /// </summary>
    public partial class SingleItemLoading : SingleItemBase
    {
        public SingleItemLoading()
        {
            InitializeComponent();

            mainGrid.DataContext = this;
        }

        
        public ClipboardViewSubItemDimension ViewItemDimensions
        {
            get { return (ClipboardViewSubItemDimension)GetValue(ViewItemDimensionsProperty); }
            set { SetValue(ViewItemDimensionsProperty, value); }
        }

        public static readonly DependencyProperty ViewItemDimensionsProperty =
            DependencyProperty.Register(nameof(ViewItemDimensions), typeof(ClipboardViewSubItemDimension), typeof(SingleItemLoading), new PropertyMetadata(null));



        public bool IsShownIntoView
        {
            get { return (bool)GetValue(IsShownIntoViewProperty); }
            set { SetValue(IsShownIntoViewProperty, value); }
        }

        public static readonly DependencyProperty IsShownIntoViewProperty =
            DependencyProperty.Register(nameof(IsShownIntoView), typeof(bool), typeof(SingleItemLoading), new PropertyMetadata(false));



        public bool IsLoadingIndicatorFadeInEnabled
        {
            get { return (bool)GetValue(IsLoadingIndicatorFadeInEnabledProperty); }
            set { SetValue(IsLoadingIndicatorFadeInEnabledProperty, value); }
        }
        
        public static readonly DependencyProperty IsLoadingIndicatorFadeInEnabledProperty =
            DependencyProperty.Register(nameof(IsLoadingIndicatorFadeInEnabled), typeof(bool), typeof(SingleItemLoading), new PropertyMetadata(false));


    }
}
