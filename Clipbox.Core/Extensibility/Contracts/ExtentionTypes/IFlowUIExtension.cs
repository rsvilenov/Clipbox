using Clipbox.Core.ClipboardContent.Interfaces;
using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public interface IFlowUIExtension : IHaveFlowParameters, IClipboxExtension
    {
        /// <summary>
        /// Determines whether the extension should be shown
        /// </summary>
        /// <param name="selectedItems">A collection of the selected clipboard items</param>
        /// <returns>True if the extension should be shown.</returns>
        bool OnShouldShow(IEnumerable<IClipboardItem> selectedItems);

        /// <summary>
        /// Sets the data which will be used by the extension and creates UI.
        /// </summary>
        /// <param name="selectedItems">The selected clipboard items, upon which the extension will operate.</param>
        /// <param name="maxWidth">The maximum width the extension can occupy.</param>
        /// <param name="maxHeigt">The maximum height the extension can occupy.</param>
        /// <returns>The created extension UI control.</returns>
        IFlowExtensionUIControl CreateExtensionUI(IEnumerable<IClipboardItem> selectedItems, double maxWidth, double maxHeigt);

        /// <summary>
        /// Called when the UI extension is unloaded
        /// </summary>
        void Destroy();
    }
}
