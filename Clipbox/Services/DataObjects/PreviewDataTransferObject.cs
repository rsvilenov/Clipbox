using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Clipbox.Services.DataObjects
{
    [Serializable]
    public class PreviewDataTransferObject
    {
        public PreviewDataTransferObject()
        {
            PreviewItems = new List<PreviewItemTransferObject>();
        }

        public string ClipboardItemId { get; set; }

        public bool IsGroup { get; set; }

        public int NumberOfItemsWithoutPreview { get; set; }

        public string GeneratorExtensionTypeName { get; set; }

        public List<PreviewItemTransferObject> PreviewItems { get; set; }

        public byte[] ToByteArray()
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, this);
                return ms.ToArray();
            }
        }

        public static PreviewDataTransferObject FromByteArray(byte[] byteArray)
        {
            using (var ms = new MemoryStream(byteArray))
            {
                var formatter = new BinaryFormatter();
                return (PreviewDataTransferObject)formatter.Deserialize(ms);
            }
        }
    }
}
