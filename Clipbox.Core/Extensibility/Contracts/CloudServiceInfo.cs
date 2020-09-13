using System;

namespace Clipbox.Core.Extensibility.Contracts
{
    public class CloudServiceInfo
    {
        public CloudServiceInfo(string serviceName,
            string serviceURI,
            bool isAuthenticated,
            bool isHealthy,
            string failureReason)
        {
            if (!isHealthy && string.IsNullOrWhiteSpace(failureReason))
            {
                throw new ArgumentException($"You have to provide a value for {nameof(failureReason)} when {nameof(isHealthy)} is false.");
            }

            ServiceName = serviceName;
            ServiceURI = serviceURI;
            IsHealthy = isHealthy;
            FailureReason = failureReason;
        }

        public CloudServiceInfo(string serviceName,
           string serviceURI,
           bool isAuthenticated,
           bool isHealthy,
           Exception exception)
        {
            ServiceName = serviceName;
            ServiceURI = serviceURI;
            IsHealthy = isHealthy;
            Exception = exception;
            FailureReason = exception?.Message;
        }

        public string ServiceName { get; }

        public string ServiceURI { get; }

        public bool IsHealthy { get; }

        public string FailureReason { get; }

        public Exception Exception { get; }
    }
}
