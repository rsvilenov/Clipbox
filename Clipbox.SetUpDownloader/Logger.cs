using System;
using System.IO;

namespace Clipbpx.SetUpDownloader
{
    public class Logger
    {
        private string _logFilePath;
        public Logger()
        {
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
            File.AppendAllText(_logFilePath, $"{DateTime.Now.ToFileTime()} --> {message}{Environment.NewLine}");
        }
    }
}
