using System;
using System.Linq;

namespace Clipbox.Infrastructure.ExtentionMethods
{
    public static class StringExtentions
    {
        public static bool EndsWithAny(this string str, params string[] end)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (end == null || end.Any(e => e == null))
            {
                throw new ArgumentNullException("end");
            }

            foreach (string e in end)
            {
                if (str.EndsWith(e))
                    return true;
            }

            return false;
        }
    }
}
