namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IExtensionInfo
    {
        double Version { get; }

        string Author { get; }

        string Description { get; }
    }
}
