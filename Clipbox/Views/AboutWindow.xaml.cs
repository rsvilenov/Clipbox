using Clipbox.Core;
using Clipbox.Infrastructure.RegistryAccess;
using Clipbox.Infrastructure.UI.CustomControls;
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
            VersionString = RegistryUtils.GetProductVersion();
        }

        public string VersionString
        {
            get { return (string)GetValue(VersionStringProperty); }
            set { SetValue(VersionStringProperty, value); }
        }

        public static readonly DependencyProperty VersionStringProperty =
            DependencyProperty.Register(nameof(VersionString), typeof(string), typeof(AboutWindow), new PropertyMetadata(string.Empty));

        private void HyperlinkEula_Click(object sender, RoutedEventArgs e)
        {
            using (Process p = Process.Start(ProductConstants.PRODUCT_EULA_URL)) ;
            e.Handled = true;
        }

        private void HyperlinkPrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            using (Process p = Process.Start(ProductConstants.PRODUCT_PRIVACY_POLICY_URL)) ;
            e.Handled = true;
        }
    }
}
