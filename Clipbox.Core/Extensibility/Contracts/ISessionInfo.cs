namespace Clipbox.Core.Extensibility.Contracts
{
    public interface ISessionInfo
    {
        string SessionUniqueId { get; }

        string UserName { get; }
    }
}
