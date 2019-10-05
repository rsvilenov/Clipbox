using System;

namespace Clipbox.Core.Extensibility.Contracts.PreviewData
{
    public class HtmlPreviewData : ImagePreviewData
    {
        /// <summary>
        /// Sometimes the clipboard html data contains a comment with the url
        /// </summary>
        public string OriginatingUrl { get; set; }
    }
}
