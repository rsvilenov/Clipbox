using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System;
using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public abstract class FlowUIExtension<TExtensionUIControl> : ParametrizedExtension, IFlowUIExtension
        where TExtensionUIControl : IFlowExtensionUIControl
    {
        /// <summary>
        /// <see cref="IFlowUIExtension.OnShouldShow(IEnumerable{IClipboardItem})"/>
        /// </summary>
        public abstract bool OnShouldShow(IEnumerable<IClipboardItem> selectedItems);

        /// <summary>
        /// Called when the UI wants to show the UI for our extension
        /// </summary>
        /// <param name="extensionUI">The constructed UI control for the extension.</param>
        /// <param name="selectedItems">The selected clipboard items, upon which the extension will operate.</param>
        /// <param name="maxWidth">The maximum width the extension can occupy.</param>
        /// <param name="maxHeigt">The maximum height the extension can occupy.</param>
        protected abstract void OnExtensionUICreated(TExtensionUIControl extensionUIControl, IEnumerable<IClipboardItem> selectedItems, double maxWidth, double maxHeigt);

        /// <summary>
        /// Allows adding some cleanup code when the clipboard view item is destroyed.
        /// </summary>
        /// <param name="extensionUIControl">The constructed UI control for the extension.</param>
        protected virtual void OnExtensionUIDestroying(TExtensionUIControl extensionUIControl)
        {
        }

        /// <summary>
        /// <see cref="IFlowUIExtension.Destroy"/>
        /// </summary>
        public void Destroy()
        {
            OnExtensionUIDestroying(_extensionUIControl);
            ClearParameters();
        }

        /// <summary>
        /// <see cref="IFlowUIExtension.LoadExtensionUI(IEnumerable{IClipboardItem}, double, double)"/>
        /// </summary>
        public IFlowExtensionUIControl CreateExtensionUI(IEnumerable<IClipboardItem> selectedItems, double maxWidth, double maxHeigt)
        {
            _extensionUIControl = (TExtensionUIControl)Activator.CreateInstance(typeof(TExtensionUIControl));
            OnExtensionUICreated(_extensionUIControl, selectedItems, maxWidth, maxHeigt);
            return _extensionUIControl;
        }

        private TExtensionUIControl _extensionUIControl;
    }
}
