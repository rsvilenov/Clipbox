using System;

namespace Clipbox.Core.Diagnostics
{
    [Serializable]
    public class FailureInfo
    {
        public FailureInfo(Exception ex)
            : this()
        {
            if (ex is null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            ExceptionType = ex.GetType().ToString();
            ExceptionMessage = ex.Message?.ToString();
            ExceptionStringData = ex.ToString();
        }

        public FailureInfo(string failureMessage)
            : this()
        {
            FailureMessage = failureMessage;
        }

        private FailureInfo()
        {
            UtcTicks = DateTime.UtcNow.Ticks;
            LocalTicks = DateTime.Now.Ticks;
        }

        public string ExceptionType { get; set; }

        public string ExceptionMessage { get; set; }

        public string ExceptionStringData { get; set; }

        /// <summary>
        /// In case the error is not an exception
        /// </summary>
        public string FailureMessage { get; }
        
        public long UtcTicks { get; set; }

        public long LocalTicks { get; set; }
    }
}
