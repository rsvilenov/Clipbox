using Clipbox.WixSetup.View;
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
        private const string DEFAULT_LANG_CODE = "en-US";

        private readonly LocalizationHelper _localizationHelper;
        private readonly InstallViewModel _viewModel;

        public InstallWindow(InstallViewModel vm)
        {
            InitializeComponent();

            _viewModel = vm;

            _localizationHelper = new LocalizationHelper(this);
            _localizationHelper.SetLanguage(DEFAULT_LANG_CODE);
            this.DataContext = vm;
            Closed += InstallWindow_Closed;
        }

        private void InstallWindow_Closed(object sender, EventArgs e)
        {
            _viewModel.QuitInstaller();
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void CaptionGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void BtnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnInstalloptionsBack_Click(object sender, RoutedEventArgs e)
        {
            FirstPage.Visibility = Visibility.Visible;
            CustomInstallPage.Visibility = Visibility.Collapsed;
        }

        private void Customize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FirstPage.Visibility = Visibility.Collapsed;
            CustomInstallPage.Visibility = Visibility.Visible;
        }

        private void BtnChangeInstallDirectory_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

            }
        }

        private void ComboBoxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string newLanguageCode = ((ComboBoxItem)ComboBoxLanguage.SelectedItem).Tag as string;
            _localizationHelper.SetLanguage(newLanguageCode);
        }
    }
}
