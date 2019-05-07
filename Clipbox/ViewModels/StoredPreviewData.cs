using System;
using System.Collections.Generic;

namespace Clipbox.ViewModels
{
    [Serializable]
    public class StoredPreviewData
    {
        public StoredPreviewData()
        {
            PreviewItems = new List<StoredPreviewItem>();
        }

        public bool IsGroup { get; set; }

        public int NMore { get; set; }
        
        public List<StoredPreviewItem> PreviewItems { get; set; }
    }
}
