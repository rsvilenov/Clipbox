using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Extensions.BuiltIn.HdropFormat.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.HdropFormat
{
    internal static class Common
    {
        public static string GetNullDelimitedString(IReadOnlyCollection<string> stringArray)
        {
            string zeroDelimitedString = stringArray.
                Select(str => str + char.MinValue).
                Aggregate((str1, str2) => str1 + str2);

            return zeroDelimitedString + char.MinValue;
        }

    }
}
