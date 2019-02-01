namespace Clipbox.Settings.Enums
{
    public enum MainClipboardViewBackgroundRenderMode
    {
        /// <summary>
        /// The background is the desktop wallpaper.
        /// Consumes less memory and more CPU due to AllowTransparency=true,
        /// which is needed by the actual wpf window on top of the GDI one.
        /// </summary>
        GDI,
        /// <summary>
        /// The background is the desktop wallpaper.
        /// Consumes less CPU and more memory due to the loading of the bitmap image
        /// of the wallpaper into the system memory
        /// </summary>
        ImageBrush,
        /// <summary>
        /// The background is a blurred version of the windows under the main clipboard view.
        /// </summary>
        Acryllic
    }
}
