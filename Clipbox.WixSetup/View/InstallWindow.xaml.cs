using Setup.WixSetup.ViewModel;
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

namespace Setup.WixSetup.View
{
    /// <summary>
    /// Interaction logic for InstallWindow.xaml
    /// </summary>
    public partial class InstallWindow : Window
    {
        public InstallWindow(InstallViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            //if (Mouse.LeftButton != MouseButtonState.Pressed)
            //{
            //    return;
            //}

            //DragMove();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
