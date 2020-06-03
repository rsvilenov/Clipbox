using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Clipbox.Infrastructure.Serialization
{
    public static class BinaryFormatterSerialization
    {
        public static byte[] SerializeObject(object item)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, item);
                ms.Flush();
                return ms.ToArray();
            }
        }

        public static T DeserializeObject<T>(byte[] itemBytes)
        {
            using (MemoryStream ms = new MemoryStream(itemBytes))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(ms);
            }
        }
    }
}
