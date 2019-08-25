using Clipbox.Core.Extensibility.Enums;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IHouseKeepingTask
    {
        /// <summary>
        /// Sets the clipboard format for which the housekeeping task should be performed
        /// </summary>
        string ClipboardFormat { get; }

        /// <summary>
        /// Determines whether the IClipboardItem should be unshrinked in order to be processed. 
        /// Some tasks may not require that, for example tasks that remove the item after certain period.
        /// </summary>
        bool RequiresRestoredState { get; }

        /// <summary>
        /// Determines how often the task should be performed.
        /// </summary>
        HouseKeepingRegularity Regularity { get; }
    }
}
