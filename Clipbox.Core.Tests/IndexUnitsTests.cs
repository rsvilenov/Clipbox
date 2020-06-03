using Clipbox.Core.Search;
using Clipbox.Core.Search.IndexUnits;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.Serialization;
using NUnit.Framework;
using System.Linq;

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
            byte[] serialized = BinaryFormatterSerialization.SerializeObject(obj);
            T deserialized = default(T);
            Assert.DoesNotThrow(() =>
            {
                deserialized = BinaryFormatterSerialization.DeserializeObject<T>(serialized);
            });

            Assert.IsNotNull(deserialized);
            Assert.IsInstanceOf<T>(deserialized);
            Assert.IsNotEmpty(deserialized.StringValues);
            Assert.IsTrue(deserialized.StringValues.Count() == obj.StringValues.Count());
            Assert.IsTrue(deserialized.StringValues.SequenceEqual(obj.StringValues));
        }
    }
}
