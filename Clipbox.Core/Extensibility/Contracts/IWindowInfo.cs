namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IWindowInfo
    {
        string Caption { get; }

        byte[] Icon { get; }

        string ApplicationPath { get; }
    }
}
