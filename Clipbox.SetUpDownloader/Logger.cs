using System;
using System.IO;

namespace Clipbpx.SetUpDownloader
{
    public class Logger
    {
        private string _logFilePath;
        private object _lockObject;
        public Logger()
        {
            _lockObject = new object();

            string tempFileName = Path.GetTempFileName();

            _logFilePath =
                Path.Combine
                (
                    Path.GetDirectoryName(tempFileName), 
                    $"{ProductConstants.PRODUCT_NAME}_downloader_{Path.GetFileNameWithoutExtension(tempFileName)}.log"
                );

            File.Move(tempFileName, _logFilePath);
        }

        public void Log(string message)
        {
            lock (_lockObject)
            {
                try
                {
                    File.AppendAllText(_logFilePath, $"{DateTime.Now.ToFileTime()} --> {message}{Environment.NewLine}");
                }
                catch 
                {
                    // the crucial task in this program is downloading, not logging, so... just swallow the exception
                }
            }
        }
    }
}
