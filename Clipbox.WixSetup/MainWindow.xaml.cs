using Clipbox.WixSetup.ViewModel;
using System.Windows;

namespace Clipbox.WixSetup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public InstallViewModel InstallVm { get; set; }
        public ManagedBA Mba { get; set; }
    }
}
