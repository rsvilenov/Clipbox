namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IExtentionInfo
    {
        double Version { get; }

        string Author { get; }

        string Description { get; }
    }
}
