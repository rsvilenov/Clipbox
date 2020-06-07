using System;

namespace Clipbox.Infrastructure.Search
{
    [Serializable]
    public class IndexHealthInfo
    {
        public IndexHealthInfo(bool isHealthy)
        {
            IsHealthy = isHealthy;
        }

        public IndexHealthInfo(bool isHealthy, string errorInfo)
        {
            IsHealthy = isHealthy;
            ErrorInfo = errorInfo;
        }

        // for deserialization
        private IndexHealthInfo() { }

        public bool IsHealthy { get; }

        public string ErrorInfo { get; }
    }
}
