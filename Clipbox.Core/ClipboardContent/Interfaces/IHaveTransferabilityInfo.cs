namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IHaveTransferabilityInfo
    {
        /// <summary>
        /// Indicates whether the data can have any meaning on another computer.
        /// Filesystem paths, preferred drop effect and the like can't be transfered.
        /// </summary>
        bool CanTransfer { get; }
        
        /// <summary>
        /// Indicates whether the data should be transfered to another computer.
        /// </summary>
        bool ShouldTransfer { get; }
    }
}
