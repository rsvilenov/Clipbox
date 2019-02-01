using System.Collections.Generic;
using System.IO;

namespace Clipbox.Infrastructure.Persistence
{
    public interface ICloudService
    {
        void CreateFolder(string remoteFolderName);

        void UploadFile(string remoteFolderName, string remoteFileName, Stream stream);

        void DownloadFile(string remoteFolderName, string remoteFileName, Stream stream);

        IEnumerable<string> ListFiles(string remoteFolderName, string fileNamePart = "");

        void DeleteFile(string remoteFolderName, string remoteFileName);

        void DeleteFolder(string remoteFolderName);

        bool CheckFolderExists(string driveFolderName);

        bool CheckFileExists(string driveFolderName, string driveFileName);
    }
}
