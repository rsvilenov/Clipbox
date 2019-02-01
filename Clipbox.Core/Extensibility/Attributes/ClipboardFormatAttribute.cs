using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Extensibility.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ClipboardFormatAttribute : Attribute
    {
        public ClipboardFormatAttribute(string format)
        {
            Format = format;
        }

        /// <summary>
        /// One of the predefined formats (<see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ff729168(v=vs.85).aspx">MSDN</see>)
        /// or a custom format.
        /// </summary>
        public string Format { get; }

        public static bool IsDefinedFor(Type type)
        {
            var formatAttributes = (ClipboardFormatAttribute[])type.GetCustomAttributes(typeof(ClipboardFormatAttribute), true);
            return formatAttributes.Any();
        }

        public static IEnumerable<string> GetFormats(Type type)
        {
            var formatAttributes = (ClipboardFormatAttribute[])type.GetCustomAttributes(typeof(ClipboardFormatAttribute), true);
            return formatAttributes.Select(atr => atr.Format);
        }

    }
}
