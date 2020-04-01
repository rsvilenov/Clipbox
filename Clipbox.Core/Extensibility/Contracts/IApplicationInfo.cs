namespace Clipbox.Core.Extensibility.Contracts
{
    /// <summary>
    /// Information about the application;
    /// Each clipboard item will have 
    /// its own application info and they 
    /// will be the same for many clipboard items.
    /// However, .NET uses string inerning, so
    /// storing many object of this type with the same
    /// info should not take up a lot of RAM.
    /// </summary>
    public interface IApplicationInfo
    {
        string ApplicationPath { get; }

        string ProductName { get; }

        string ProductVersion { get; }

        string FileDescription { get; }

        string CompanyName { get; }
    }
}
