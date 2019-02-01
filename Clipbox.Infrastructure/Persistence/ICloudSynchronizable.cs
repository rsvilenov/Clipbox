namespace Clipbox.Infrastructure.Persistence
{
    public interface ICloudSynchronizable
    {
        /// <summary>
        /// Is the object synchronized with a cloud service
        /// </summary>
        bool IsSynchronizedWithCloud { get; set; }
    }
}
