using System.Linq;

namespace Clipbox.Extentions.BuiltIn.TextFormats
{
    public static class StringExtensions
    {
        public static string ReplaceNonAlphaWith(this string text, char replacementChar)
        {
            // replace the non-alphanumeric symbols with space.
            // based on ata's answer here: https://stackoverflow.com/questions/3210393/how-do-i-remove-all-non-alphanumeric-characters-from-a-string-except-dash
            char[] charArrayWithoutNonAlphaSymbols = text
                .Select(c => (char.IsLetterOrDigit(c) ||
                             char.IsWhiteSpace(c) ||
                             c == '-') ? c : ' ').ToArray();

            return new string(charArrayWithoutNonAlphaSymbols);
        }
    }
}
