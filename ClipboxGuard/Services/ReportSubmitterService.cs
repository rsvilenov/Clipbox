using Clipbox.Core;
using Clipbox.Core.Diagnostics;
using Clipbox.Infrastructure.Serialization;
using ClipboxGuard.Enums;
using ClipboxGuard.Services.EventArgsClasses;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Net;
using System.Text;
using System.Timers;

namespace ClipboxGuard.Services
{
    public class ReportSubmitterService : IReportSubmitterService, IDisposable
    {
        private const int TIMEOUT_MS = 30000;

        private readonly WebClient _webClient;
        private readonly Timer _timeoutTimer;
        private bool _isDisposed;
        private ReportSubmitState _currentSumbitState;
        private string _currentError;
        private bool _isTimeoutReached;

        public event EventHandler<ReportStateEventArgs> StateChanged;

        public ReportSubmitterService()
        {
            _webClient = new WebClient();
            _timeoutTimer = new Timer(TIMEOUT_MS);
            _timeoutTimer.Elapsed += _timeoutTimer_Elapsed;
        }

        private void _timeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _currentError = "Timeout";
            _isTimeoutReached = true;
            _timeoutTimer.Stop();

            _currentSumbitState = ReportSubmitState.Error;
            StateChanged?.Invoke(this, new ReportStateEventArgs(_currentSumbitState));
            
            try
            {
                _webClient.CancelAsync();
            }
            catch 
            { 
                //the docs don't say anything about possible exceptoins, but anyway...
            }
        }

        public void SubmitReport(AggregateReport aggregateReport)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ReportSubmitterService));
            }

            if (aggregateReport == null)
            {
                throw new ArgumentNullException(nameof(aggregateReport));
            }

            _isTimeoutReached = false;

            
            string reportXml = XmlSerialization.Serialize(aggregateReport);

            _webClient.Headers["Content-Type"] = "text/plain";
            _webClient.UploadDataCompleted += _webClient_UploadDataCompleted;
            _webClient.UploadProgressChanged += _webClient_UploadProgressChanged;

            _currentSumbitState = ReportSubmitState.Connecting;
            StateChanged?.Invoke(this, new ReportStateEventArgs(_currentSumbitState));

            _timeoutTimer.Start();
            _webClient.UploadDataAsync(new Uri(ProductConstants.FEEDBACK_AUTO_BUG_REPORT_URL), "POST", Encoding.UTF8.GetBytes(reportXml));

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
            if (_isTimeoutReached)
            {
                // this case is handled by the timer's elapsed event
                return;
            }

            if (e.Error != null)
            {
                _timeoutTimer.Stop();
                _currentError = e.Error.Message;
                _currentSumbitState = ReportSubmitState.Error;
                StateChanged?.Invoke(this, new ReportStateEventArgs(_currentSumbitState));
            }
            else
            {
                _timeoutTimer.Stop();
                _currentSumbitState = ReportSubmitState.Done;
                StateChanged?.Invoke(this, new ReportStateEventArgs(_currentSumbitState));
            }
        }


        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _timeoutTimer.Dispose();
                _webClient.Dispose();
            }
        }
    }
}
