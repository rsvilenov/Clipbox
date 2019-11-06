using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Linq;

namespace Clipbox.Core.Extensibility.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PreviewPriorityAttribute : Attribute
    {
        public PreviewPriorityAttribute(int priorityLevel)
        {
            PriorityLevel = priorityLevel;
        }

        public int PriorityLevel { get; }

        public static bool IsDefinedFor(Type type)
        {
            var attributes = (PreviewPriorityAttribute[])type.GetCustomAttributes(typeof(PreviewPriorityAttribute), true);
            return attributes.Any();
        }

        public static int GetPreviewPriorityLevelFor(Type extensionType)
        {
            if (!extensionType.GetInterfaces().Contains(typeof(IClipboxExtention)))
            {
                throw new InvalidCastException($"{extensionType.Name} does not implement {nameof(IClipboxExtention)}");
            }

            var attributes = (PreviewPriorityAttribute[])extensionType.GetCustomAttributes(typeof(PreviewPriorityAttribute), true);
            if (!attributes.Any())
            {
                throw new InvalidOperationException($"{ extensionType.Name } does not have the attribute {nameof(PreviewPriorityAttribute)}");
            }

            return attributes.Single().PriorityLevel;
        }
    }
}
