using System;
using System.Collections.Generic;

namespace Clipbox.Services.DataObjects
{
    [Serializable]
    public class PreviewDataTransferObject
    {
        public PreviewDataTransferObject()
        {
            PreviewItems = new List<PreviewItemTransferObject>();
        }

        public bool IsGroup { get; set; }

        public int NMore { get; set; }

        public List<PreviewItemTransferObject> PreviewItems { get; set; }
    }
}
