namespace Clipbox.Services.Enums
{
    public enum PreviewGenerationStatus
    {
        /// <summary>
        /// This status may indicate a problem in <see cref="PreviewGenerationService"/>
        /// </summary>
        Unknown,
        /// <summary>
        /// The generation of the preview has completed successfully.
        /// </summary>
        Success,
        /// <summary>
        /// Indicates that the generation took more than the maximum time
        /// defined as a setting.
        /// </summary>
        Timeout,
        /// <summary>
        /// Indicates that there were no preview generation extension
        /// for that item.
        /// </summary>
        NoGeneratorFound,
        /// <summary>
        /// Indicates that the preview handler returned null or that the object that it returned
        /// could not be converted to a meaningful image data.
        /// </summary>
        NoBitmapDataGenerated
    }
}
