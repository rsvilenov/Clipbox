using Clipbox.Services.Interfaces;

namespace Clipbox.Views.Settings.Interfaces
{
    public interface ICloudSyncServiceConsumer
    {
        void SetCloudSyncService(ICloudSynchronizationService cloudSyncSvc);
    }
}
