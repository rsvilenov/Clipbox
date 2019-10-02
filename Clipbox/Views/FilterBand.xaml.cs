using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for FilterBand.xaml
    /// </summary>
    public partial class FilterBand : UserControl
    {
        public FilterBand()
        {
            InitializeComponent();
        }

        private void TypeExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            UncheckChildToggleButtons((Expander)sender);
        }

        private void CloudExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            UncheckChildToggleButtons((Expander)sender);
        }

        private static void UncheckChildToggleButtons(Expander sender)
        {
            List<UIElement> toggleButtons = Traversing.GetVisualChildCollection(sender, (child) => child is ToggleButton);
            foreach (UIElement child in toggleButtons)
            {
                var tglBtn = child as ToggleButton;
                if (tglBtn != null)
                {
                    tglBtn.IsChecked = false;
                }
            }
        }
    }
}
