using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Clipbox.Technical.ExtensionMethods
{
    public static class DateTimeExtensions
    {
        private static DatePatternForCulture LondDatePatternForCultureNoDayName;

        public static string ToWindowsStyleHumanReadableString(this DateTime date)
        {
            string humanReadableDateString = string.Empty;

            // TODO: translate "Today" and "Yesterday"
            if (date.Date == DateTime.Today)
            {
                humanReadableDateString = "Today";
            }
            else if (date.Date == DateTime.Today.AddDays(-1))
            {
                humanReadableDateString = "Yesterday";
            }

            else if (date.Year == DateTime.Now.Year)
            {
                // If the date is from the current year, display the Month/Day pattern
                humanReadableDateString = date.ToString("M");
            }
            else
            {
                // if not, show the long date pattern, but strip the day name away
                if (LondDatePatternForCultureNoDayName == null 
                    || LondDatePatternForCultureNoDayName.Culture != CultureInfo.CurrentCulture)
                {
                    string longDatePattern = CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;

                    LondDatePatternForCultureNoDayName = new DatePatternForCulture
                    {
                        Culture = CultureInfo.CurrentCulture,
                        DatePattern = Regex.Replace(longDatePattern, @"[\s,\.]*dddd[\s,\.]*", "")
                    };
                }

                humanReadableDateString = date.ToString(LondDatePatternForCultureNoDayName.DatePattern);
            }

            return humanReadableDateString;
        }

        private class DatePatternForCulture
        {
            public CultureInfo Culture { get; set; }
            public string DatePattern { get; set; }
        }
    }
}
