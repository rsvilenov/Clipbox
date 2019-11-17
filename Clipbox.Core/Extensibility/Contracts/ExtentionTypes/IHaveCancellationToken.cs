using System.Threading;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public interface IHaveCancellationToken
    {
        CancellationToken CancellationToken { get; set; }
    }
}
