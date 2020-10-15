using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
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
using Clipbox.Infrastructure.ExtensionMethods;
using ClipboxGuard.Enums;

namespace ClipboxGuard
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {

        public ErrorWindow(string errorDetails, ErrorOrigin errorOrigin, bool showRestartOption)
        {
            InitializeComponent();
            mainGrid.DataContext = this;
            try
            {
                ErrorOriginLabel = string.Join("; ", errorOrigin.GetDescriptionsForFlagsEnum());
            }
            catch
            {
                ErrorOriginLabel = "Unknown";
            }

            ErrorText = errorDetails;
            ShowRestartOption = showRestartOption;
            IsRestartRequested = true;
            IsReportErrorRequested = NetworkInterface.GetIsNetworkAvailable();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private string ErrorText
        {
            get { return (string)GetValue(ErrorTextProperty); }
            set { SetValue(ErrorTextProperty, value); }
        }

        private static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(ErrorWindow), new PropertyMetadata(string.Empty));




        private bool ShowRestartOption
        {
            get { return (bool)GetValue(ShowRestartOptionProperty); }
            set { SetValue(ShowRestartOptionProperty, value); }
        }
        
        private static readonly DependencyProperty ShowRestartOptionProperty =
            DependencyProperty.Register(nameof(ShowRestartOption), typeof(bool), typeof(ErrorWindow), new PropertyMetadata(false));
        

        public bool IsRestartRequested
        {
            get { return (bool)GetValue(IsRestartRequestedProperty); }
            set { SetValue(IsRestartRequestedProperty, value); }
        }

        public static readonly DependencyProperty IsRestartRequestedProperty =
            DependencyProperty.Register(nameof(IsRestartRequested), typeof(bool), typeof(ErrorWindow), new PropertyMetadata(false));

        
        public bool IsReportErrorRequested
        {
            get { return (bool)GetValue(IsReportErrorRequestedProperty); }
            set { SetValue(IsReportErrorRequestedProperty, value); }
        }

        public static readonly DependencyProperty IsReportErrorRequestedProperty =
            DependencyProperty.Register(nameof(IsReportErrorRequested), typeof(bool), typeof(ErrorWindow), new PropertyMetadata(false));
        

        public string ErrorOriginLabel
        {
            get { return (string)GetValue(ErrorOriginLabelProperty); }
            set { SetValue(ErrorOriginLabelProperty, value); }
        }

        public static readonly DependencyProperty ErrorOriginLabelProperty =
            DependencyProperty.Register(nameof(ErrorOriginLabel), typeof(string), typeof(ErrorWindow), new PropertyMetadata(string.Empty));




        private ReportSendState StateOfReportSending
        {
            get { return (ReportSendState)GetValue(StateOfReportSendingProperty); }
            set { SetValue(StateOfReportSendingProperty, value); }
        }

        private static readonly DependencyProperty StateOfReportSendingProperty =
            DependencyProperty.Register(nameof(StateOfReportSending), typeof(ReportSendState), typeof(ErrorWindow), new PropertyMetadata(default(ReportSendState)));
        
    }
}
