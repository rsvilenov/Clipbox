namespace Clipbox.Services.Enums
{
    public enum PreviewGenerationStatus
    {
        Unknown,
        Success,
        /// <summary>
        /// Indicates that the generation took more than the maximum time
        /// defined in <see cref="PreviewGenerationService.PREVIEW_GENERATION_TIMEOUT_MS"/>
        /// </summary>
        Timeout,
        /// <summary>
        /// Indicates that there were no preview generation extension
        /// for that item
        /// </summary>
        NoGeneratorFound,
        /// <summary>
        /// Indicates that the preview handler returned null or that the object that it returned
        /// could not be converted to a meaningful image data.
        /// </summary>
        NoBitmapDataGenerated,
        Error
    }
}
