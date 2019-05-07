using System;

namespace Clipbox.ViewModels
{
    [Serializable]
    public class StoredPreviewItem
    {
        public byte[] ImageData { get; set; }

        public string Text { get; set; }

        public string Description { get; set; }
    }
}
