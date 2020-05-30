using Clipbox.Core.Search;
using Clipbox.Core.Search.IndexUnits;
using Clipbox.Core.Search.SearchTerms;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.Core.Tests
{
    [TestFixture]
    public class IndexUnitsTests
    {
        [Test]
        public void TestTextIndexUnitSerialization()
        {
            TestSerialization(new TextIndexUnit(new string[] { "test1", "test2" }));
            TestSerialization(new TextSearchTerm(new string[] { "test1", "test2" }));
        }

        void TestSerialization<T>(T obj)
            where T : ISearchObject
        {
            byte[] serialized = SerializeObject(obj);
            T deserialized = default(T);
            Assert.DoesNotThrow(() =>
            {
                deserialized = DeserializeObject<T>(serialized);
            });

            Assert.IsNotNull(deserialized);
            Assert.IsInstanceOf<T>(deserialized);
            Assert.IsNotEmpty(deserialized.StringValues);
            Assert.IsTrue(deserialized.StringValues.Count() == obj.StringValues.Count());
            Assert.IsTrue(deserialized.StringValues.SequenceEqual(obj.StringValues));
        }

        private byte[] SerializeObject(object item)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, item);
                ms.Flush();
                return ms.ToArray();
            }
        }

        private T DeserializeObject<T>(byte[] itemBytes)
        {
            using (MemoryStream ms = new MemoryStream(itemBytes))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(ms);
            }
        }
    }
}
