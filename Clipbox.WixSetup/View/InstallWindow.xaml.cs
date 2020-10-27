using Clipbox.WixSetup;
using Clipbox.WixSetup.View;
using Clipbox.WixSetup.ViewModel;
using System;
using System.ComponentModel;
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

        private bool _isCancelQuitConfirmed;
        public InstallWindow(InstallViewModel vm)
        {
            InitializeComponent();

            _quitConfirmationMre = new ManualResetEvent(false);

            _viewModel = vm;
            _viewModel.QuitConfirmationRequested += _viewModel_QuitConfirmationRequested;
            _viewModel.CancelConfirmationRequested += _viewModel_CancelConfirmationRequested;
            _viewModel.DetectComplete += _viewModel_DetectComplete;

            this.MainGrid.DataContext = vm;

            _localizationHelper = new LocalizationHelper(this);
            _localizationHelper.SetLanguage(DEFAULT_LANG_CODE);

            Closing += InstallWindow_Closing;
        }

        private void _viewModel_DetectComplete(object sender, EventArgs e)
        {
            string cultureCodeToSet = _viewModel.CultureCode ?? DEFAULT_LANG_CODE;

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                try
                {
                    _localizationHelper.SetLanguage(cultureCodeToSet);

                    ComboBoxItem itemToSelect = null;

                    foreach (ComboBoxItem item in ComboBoxLanguage.Items)
                    {
                        if (item.Tag as string == cultureCodeToSet)
                        {
                            itemToSelect = item;
                        }
                    }

                    if (itemToSelect != null)
                    {
                        itemToSelect.IsSelected = true;
                    }
                }
                catch (Exception ex)
                {
                    _viewModel.LogMessage($"Error while trying to set the initial language: {ex}");
                }
            }));
        }

        private void InstallWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (_viewModel.QuitCommand.CanExecute(null))
            {
                _viewModel.QuitCommand.Execute(null);
            }
        }

        private void _viewModel_CancelConfirmationRequested(object sender, CancelEventArgs e)
        {
            ShowAndWaitCancelQuitConfirmationDialog();
            e.Cancel = !_isCancelQuitConfirmed;
        }

        private void _viewModel_QuitConfirmationRequested(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ShowAndWaitCancelQuitConfirmationDialog();
            e.Cancel = !_isCancelQuitConfirmed;
        }

        private void ShowAndWaitCancelQuitConfirmationDialog()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                            new Action(() =>
                            {
                                DisableCurtain.Visibility = Visibility.Visible;
                                ConfirmQuitPopup.IsOpen = true;
                            }));
            _quitConfirmationMre.Reset();
            _quitConfirmationMre.WaitOne();
        }

        private void BtnConfirmQuitYes_Click(object sender, RoutedEventArgs e)
        {
            _isCancelQuitConfirmed = true;

            DisableCurtain.Visibility = Visibility.Collapsed;
            ConfirmQuitPopup.IsOpen = false;

            _quitConfirmationMre.Set();
        }

        private void BtnConfirmQuitNo_Click(object sender, RoutedEventArgs e)
        {
            _isCancelQuitConfirmed = false;

            DisableCurtain.Visibility = Visibility.Collapsed;
            ConfirmQuitPopup.IsOpen = false;

            _quitConfirmationMre.Set();
        }

        private void LicenseAgreementLink_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ProductConstants.PRODUCT_EULA_URL);
        }

        private void PrivacyPolicyLink_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ProductConstants.PRODUCT_PRIVACY_POLICY_URL);
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
                _viewModel.InstallationDirectory = System.IO.Path.Combine(fbd.SelectedPath, ProductConstants.COMPANY_NAME, ProductConstants.PRODUCT_NAME);
            }
        }

        private void ComboBoxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((ComboBoxLanguage?.SelectedItem as ComboBoxItem)?.Tag is string newCultureCode)
            {
                _localizationHelper.SetLanguage(newCultureCode);
                _viewModel.CultureCode = newCultureCode;
            }
        }

        private void ButtonRestartNow_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd",
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                Arguments = "/C shutdown -f -r -t 5"
            });
        }
    }
}
