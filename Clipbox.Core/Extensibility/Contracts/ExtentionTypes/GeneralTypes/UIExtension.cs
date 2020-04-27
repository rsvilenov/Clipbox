using Clipbox.Core.ClipboardContent.Interfaces;
using System.Collections.Generic;
using System.Windows;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public abstract class UIExtension : ParametrizedExtension
    {
        /// <summary>
        /// Determines whether the extension should be shown
        /// </summary>
        /// <param name="selectedItems">A collection of the selected clipboard items</param>
        /// <returns></returns>
        public abstract bool OnShouldShow(IEnumerable<IClipboardItem> selectedItems);

        /// <summary>
        /// Allows creating a custom ui element, such as a button, for the view item of the current clipboard item.
        /// </summary>
        /// <param name="selectedItems"></param>
        /// <param name="additionalViewElement"></param>
        /// <param name="availableSize">The size available for fitting the additional view element</param>
        public virtual void OnExtensionViewGenerating(IEnumerable<IClipboardItem> selectedItems, ref UIElement additionalViewElement, Size availableSize)
        { }

        /// <summary>
        /// Allows adding some cleanup code when the clipboard view item is destroyed.
        /// </summary>
        public virtual void OnExtensionViewDestroyed()
        { }
    }
}
