using Clipbox.WixSetup.View;
using Clipbox.WixSetup.ViewModel;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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
        private readonly ManualResetEvent _quitConfirmationMre;

        private bool _isQuitConfirmed;
        public InstallWindow(InstallViewModel vm)
        {
            InitializeComponent();

            _quitConfirmationMre = new ManualResetEvent(false);

            _viewModel = vm;
            _viewModel.QuitConfirmationRequested += _viewModel_QuitConfirmationRequested;

            this.MainGrid.DataContext = vm;

            _localizationHelper = new LocalizationHelper(this);
            _localizationHelper.SetLanguage(DEFAULT_LANG_CODE);

            Closing += InstallWindow_Closing;
        }

        private void InstallWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (_viewModel.QuitCommand.CanExecute(null))
            {
                _viewModel.QuitCommand.Execute(null);
            }
        }

        private void _viewModel_QuitConfirmationRequested(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    DisableCurtain.Visibility = Visibility.Visible;
                    ConfirmQuitPopup.IsOpen = true;
                }));
            _quitConfirmationMre.Reset();
            _quitConfirmationMre.WaitOne();
            e.Cancel = !_isQuitConfirmed;
        }

        private void BtnConfirmQuitYes_Click(object sender, RoutedEventArgs e)
        {
            _isQuitConfirmed = true;

            DisableCurtain.Visibility = Visibility.Collapsed;
            ConfirmQuitPopup.IsOpen = false;

            _quitConfirmationMre.Set();
        }

        private void BtnConfirmQuitNo_Click(object sender, RoutedEventArgs e)
        {
            _isQuitConfirmed = false;

            DisableCurtain.Visibility = Visibility.Collapsed;
            ConfirmQuitPopup.IsOpen = false;

            _quitConfirmationMre.Set();
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
            if ((ComboBoxLanguage?.SelectedItem as ComboBoxItem)?.Tag is string newLanguageCode)
            {
                _localizationHelper.SetLanguage(newLanguageCode);
            }
        }
    }
}
