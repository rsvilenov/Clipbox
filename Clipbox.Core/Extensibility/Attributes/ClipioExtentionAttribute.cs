using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Linq;

namespace Clipbox.Core.Extensibility.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ClipboxExtentionAttribute : Attribute
    {
        public string Description { get; set; }

        public string Author { get; set; }

        public double Version { get; set; }

        public static bool IsDefinedFor(Type type)
        {
            var formatAttributes = (ClipboxExtentionAttribute[])type.GetCustomAttributes(typeof(ClipboxExtentionAttribute), true);
            return formatAttributes.Any();
        }

        public static IExtentionInfo GetExtentionInfo(Type type)
        {
            var extentionAttribute = (ClipboxExtentionAttribute)type.
                GetCustomAttributes(typeof(ClipboxExtentionAttribute), true).
                FirstOrDefault();

            if (extentionAttribute == null)
            {
                throw new InvalidOperationException($"Attribute {nameof(ClipboxExtentionAttribute)} has not been defined for type {type.Name}");
            }

            return new ExtentionInfo
            {
                Author = extentionAttribute.Author,
                Description = extentionAttribute.Description,
                Version = extentionAttribute.Version
            };
        }

        private class ExtentionInfo : IExtentionInfo
        {
            public double Version { get; set; }

            public string Author { get; set; }

            public string Description { get; set; }
        }
    }
}
