namespace Clipbox.Infrastructure.UI.Windows.Enums
{
    public enum DpiScaleMode
    {
        /// <summary>
        /// Do not do any scaling
        /// </summary>
        None,
        /// <summary>
        /// Scale the window and its contents
        /// </summary>
        Full,
        /// <summary>
        /// Custom scaling of certain visual elements within the window.
        /// The scaling is handled by custom code, which should respect
        /// the setting of DpiScaleMode.None and do nothing in this case. 
        /// </summary>
        Custom
    }

}
