using Clipbox.Core;
using Clipbox.Infrastructure.RegistryAccess;
using Clipbox.Infrastructure.UI.CustomControls;
using System.Diagnostics;
using System.Linq;
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
            string versionStr = RegistryUtils.GetProductVersion();
            
            string[] versionParts = versionStr.Split('.');
            if (versionParts.Length == 4)
            {
                VersionString = $"{versionParts[0]}.{versionParts[1]}";
                BuildNumberString = versionParts[2];
            }
            else
            {
                VersionString = versionStr;
            }
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
            OpenUrl(ProductConstants.FEEDBACK_URL);
        }

        private void ButtonReportBug_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ProductConstants.REPORT_BUG_URL);
        }
        private void OpenUrl(string url)
        {
            using (Process p = Process.Start(url)) { }
        }
    }
}
