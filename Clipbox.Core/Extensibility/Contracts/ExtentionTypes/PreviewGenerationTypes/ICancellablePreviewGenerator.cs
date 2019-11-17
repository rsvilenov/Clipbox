using System.Threading;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.PreviewGenerationTypes
{
    /// <summary>
    /// Preview generation may sometimes take too long, so we can use this interface to
    /// indicate to the system that the task can be cancelled. The system will send us a
    /// cancel request after a timeout has been reached. If we do not handle the request
    /// and stop our generator within a second timeout, the system will abort the generator's thread.
    /// </summary>
    public interface ICancellablePreviewGenerator
    {
        CancellationToken CancellationToken { get; set; }
    }
}
