using Clipbox.Core;
using Clipbox.Infrastructure.RegistryAccess;
using Clipbox.Infrastructure.UI.CustomControls;
using Clipbox.Infrastructure.Versioning;
using System;
using System.Diagnostics;
using System.Windows;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : WindowFrame
    {
        public AboutWindow()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            Shown += AboutWindow_Shown;
        }

        private void AboutWindow_Shown(object sender, System.EventArgs e)
        {
            Version version = ProductVersionHelper.GetFullProductVersion();

            VersionString = $"{version.Major}.{version.Minor}";
            BuildNumberString = version.Build.ToString();
        }

        public string VersionString
        {
            get { return (string)GetValue(VersionStringProperty); }
            set { SetValue(VersionStringProperty, value); }
        }

        public static readonly DependencyProperty VersionStringProperty =
            DependencyProperty.Register(nameof(VersionString), typeof(string), typeof(AboutWindow), new PropertyMetadata(string.Empty));

        public string BuildNumberString
        {
            get { return (string)GetValue(BuildNumberStringProperty); }
            set { SetValue(BuildNumberStringProperty, value); }
        }

        public static readonly DependencyProperty BuildNumberStringProperty =
            DependencyProperty.Register("BuildNumberString", typeof(string), typeof(AboutWindow), new PropertyMetadata(string.Empty));



        private void HyperlinkEula_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ProductConstants.PRODUCT_EULA_URL);
            e.Handled = true;
        }

        private void HyperlinkPrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ProductConstants.PRODUCT_PRIVACY_POLICY_URL);
            e.Handled = true;
        }

        private void ButtonOnlineHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ProductConstants.HELP_URL);
        }

        private void ButtonFeedback_Click(object sender, RoutedEventArgs e)
        {
            string versionString = GetProductVersionString();
            OpenUrl($"{ProductConstants.FEEDBACK_URL}?version={versionString}&is64bit={Environment.Is64BitProcess}");
        }

        private void ButtonReportBug_Click(object sender, RoutedEventArgs e)
        {
            string versionString = GetProductVersionString();
            OpenUrl($"{ProductConstants.REPORT_BUG_URL}?version={versionString}&is64bit={Environment.Is64BitProcess}");
        }

        private string GetProductVersionString()
        {
            Version version = ProductVersionHelper.GetFullProductVersion();
            string versionString = $"{version.Major }.{version.Minor}.{version.Build}";
            return versionString;
        }

        private void OpenUrl(string url)
        {
            using (Process p = Process.Start(url)) { }
        }
    }
}
