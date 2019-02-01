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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClipboxGuard
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {

        public ErrorWindow(string error)
        {
            InitializeComponent();
            mainGrid.DataContext = this;
            exceptionTb.Text = error;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public bool IsRestartClientProcessCbChecked { get; set; }
    }
}
