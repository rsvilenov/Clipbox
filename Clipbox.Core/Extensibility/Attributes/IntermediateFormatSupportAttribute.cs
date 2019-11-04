using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Linq;

namespace Clipbox.Core.Extensibility.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IntermediateFormatSupportAttribute : Attribute
    {
        public IntermediateFormatSupportAttribute(string formatGuid)
        {
            if (!Guid.TryParse(formatGuid, out Guid result))
            {
                throw new InvalidOperationException($"{formatGuid} is not a valid Guid.");
            }

            FormatGuid = formatGuid;
        }

        public string FormatGuid { get; }

        public static bool IsDefinedFor(Type type)
        {
            var attributes = (IntermediateFormatSupportAttribute[])type.GetCustomAttributes(typeof(IntermediateFormatSupportAttribute), true);
            return attributes.Any();
        }

        public static string GetIntermediateFormatGuidFor(Type extensionType)
        {
            if (!extensionType.GetInterfaces().Contains(typeof(IClipboxExtention)))
            {
                throw new InvalidCastException($"{extensionType.Name} does not implement {nameof(IClipboxExtention)}");
            }

            var attributes = (IntermediateFormatSupportAttribute[])extensionType.GetCustomAttributes(typeof(IntermediateFormatSupportAttribute), true);
            if (!attributes.Any())
            {
                throw new InvalidOperationException($"{ extensionType.Name } does not have the attribute {nameof(IntermediateFormatSupportAttribute)}");
            }

            return attributes.Single().FormatGuid;
        }
    }
}
