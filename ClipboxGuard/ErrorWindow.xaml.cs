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
using ClipboxGuard.EventArgsClasses;
using ClipboxGuard.Services.Common;

namespace ClipboxGuard
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public event EventHandler<RequestSubmitReportEventArgs> SubmitReportRequested;

        private readonly ReportAnalysis _reportAnalysis;
        public ErrorWindow(ReportAnalysis reportAnalysis, ErrorOrigin errorOrigin, bool showRestartOption, bool showSubmitOption)
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

            _reportAnalysis = reportAnalysis;

            ErrorText = reportAnalysis.ErrorDetails;
            ShowRestartOption = showRestartOption;
            ShowSubmitOption = showSubmitOption;
            IsRestartRequested = true;
            IsSubmitErrorRequested = NetworkInterface.GetIsNetworkAvailable();
        }

        public void SetReportSubmitState(ReportSubmitState state)
        {
            Dispatcher.Invoke(() => StateOfReportSending = state);
        }

        public void SetReportSubmitErrorText(string errorText)
        {
            Dispatcher.Invoke(() => ReportSubmitErrorText = errorText);
        }

        private string ErrorText
        {
            get { return (string)GetValue(ErrorTextProperty); }
            set { SetValue(ErrorTextProperty, value); }
        }

        private static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(ErrorWindow), new PropertyMetadata(string.Empty));



        public bool ShowSubmitOption
        {
            get { return (bool)GetValue(ShowSubmitOptionProperty); }
            set { SetValue(ShowSubmitOptionProperty, value); }
        }

        public static readonly DependencyProperty ShowSubmitOptionProperty =
            DependencyProperty.Register(nameof(ShowSubmitOption), typeof(bool), typeof(ErrorWindow), new PropertyMetadata(true));



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

        
        public bool IsSubmitErrorRequested
        {
            get { return (bool)GetValue(IsSubmitErrorRequestedProperty); }
            set { SetValue(IsSubmitErrorRequestedProperty, value); }
        }

        public static readonly DependencyProperty IsSubmitErrorRequestedProperty =
            DependencyProperty.Register(nameof(IsSubmitErrorRequested), typeof(bool), typeof(ErrorWindow), new PropertyMetadata(false));
        

        public string ErrorOriginLabel
        {
            get { return (string)GetValue(ErrorOriginLabelProperty); }
            set { SetValue(ErrorOriginLabelProperty, value); }
        }

        public static readonly DependencyProperty ErrorOriginLabelProperty =
            DependencyProperty.Register(nameof(ErrorOriginLabel), typeof(string), typeof(ErrorWindow), new PropertyMetadata(string.Empty));




        private ReportSubmitState StateOfReportSending
        {
            get { return (ReportSubmitState)GetValue(StateOfReportSendingProperty); }
            set { SetValue(StateOfReportSendingProperty, value); }
        }

        private static readonly DependencyProperty StateOfReportSendingProperty =
            DependencyProperty.Register(nameof(StateOfReportSending), typeof(ReportSubmitState), typeof(ErrorWindow), new PropertyMetadata(default(ReportSubmitState)));




        private string ReportSubmitErrorText
        {
            get { return (string)GetValue(ReportSubmitErrorTextProperty); }
            set { SetValue(ReportSubmitErrorTextProperty, value); }
        }

        private static readonly DependencyProperty ReportSubmitErrorTextProperty =
            DependencyProperty.Register(nameof(ReportSubmitErrorText), typeof(string), typeof(ErrorWindow), new PropertyMetadata(string.Empty));



        private void CaptionGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ShowSubmitOption 
                && IsSubmitErrorRequested 
                && StateOfReportSending != ReportSubmitState.Error)
            {
                SubmitReportRequested?.Invoke(this, new RequestSubmitReportEventArgs(_reportAnalysis));
            }
            else
            {
                Close();
            }
        }

        private void BtnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyErrorDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ErrorText);
        }
    }
}
