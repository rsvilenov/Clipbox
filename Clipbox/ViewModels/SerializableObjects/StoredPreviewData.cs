using System;
using System.Collections.Generic;

namespace Clipbox.ViewModels.SerializableObjects
{
    [Serializable]
    public class StoredPreviewData
    {
        public StoredPreviewData()
        {
            PreviewItems = new List<StoredPreviewItem>();
        }

        public bool IsGroup { get; set; }

        public int NumberOfItemsWithoutPreview { get; set; }
        
        public List<StoredPreviewItem> PreviewItems { get; set; }
    }
}
