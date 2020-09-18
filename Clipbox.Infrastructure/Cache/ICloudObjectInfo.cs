namespace Clipbox.Infrastructure.Cache
{
    public interface ICloudObjectInfo
    {
        string Name { get; }

        string Id { get; }

        bool IsFolder { get; }

        string ParentName { get; }
    }
}
