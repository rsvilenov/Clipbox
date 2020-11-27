using Clipbox.Core;
using Clipbox.Core.Diagnostics;
using Clipbox.Infrastructure.Serialization;
using ClipboxGuard.Enums;
using ClipboxGuard.Services.EventArgsClasses;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Net;
using System.Text;

namespace ClipboxGuard.Services
{
    public class ReportSubmitterService : IReportSubmitterService, IDisposable
    {
        private readonly WebClient _webClient;
        private bool _isDisposed;
        private ReportSubmitState _currentSumbitState;
        private string _currentError;

        public event EventHandler<ReportStateEventArgs> StateChanged;

        public ReportSubmitterService()
        {
            _webClient = new WebClient();
        }

        public void SubmitReport(AggregateReport aggregateReport)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ReportSubmitterService));
            }

            if (aggregateReport != null)
            {
                string reportXml = XmlSerialization.Serialize(aggregateReport);

                _webClient.Headers["Content-Type"] = "application/xml";
                _webClient.UploadDataCompleted += _webClient_UploadDataCompleted;
                _webClient.UploadProgressChanged += _webClient_UploadProgressChanged;

                _currentSumbitState = ReportSubmitState.Connecting;
                StateChanged?.Invoke(this, new ReportStateEventArgs(_currentSumbitState));
                
                _webClient.UploadDataAsync(new Uri(ProductConstants.FEEDBACK_AUTO_BUG_REPORT_URL), "POST", Encoding.UTF8.GetBytes(reportXml));
            }
        }

        public string GetError()
        {
            return _currentError;
        }

        private void _webClient_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            if (_currentSumbitState != ReportSubmitState.Sending)
            {
                _currentSumbitState = ReportSubmitState.Sending;
                StateChanged?.Invoke(this, new ReportStateEventArgs(_currentSumbitState));
            }
        }

        private void _webClient_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _currentError = e.Error.Message;
                _currentSumbitState = ReportSubmitState.Error;
                StateChanged?.Invoke(this, new ReportStateEventArgs(_currentSumbitState));
            }
            else
            {
                _currentSumbitState = ReportSubmitState.Done;
                StateChanged?.Invoke(this, new ReportStateEventArgs(_currentSumbitState));
            }
        }


        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _webClient.Dispose();
            }
        }
    }
}
