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
            string humanReadableDateString;

            if (date.Date == DateTime.Today)
            {
                humanReadableDateString = "LBL_DATE_TODAY";
            }
            else if (date.Date == DateTime.Today.AddDays(-1))
            {
                humanReadableDateString = "LBL_DATE_YESTERDAY";
            }

            else if (date.Year == DateTime.Now.Year)
            {
                // If the date is from the current year, display the Month/Day pattern
                humanReadableDateString = date.ToString("M", CultureInfo.DefaultThreadCurrentUICulture);
            }
            else
            {
                // if not, show the long date pattern, but strip the day name away
                if (LondDatePatternForCultureNoDayName == null 
                    || LondDatePatternForCultureNoDayName.Culture != CultureInfo.DefaultThreadCurrentUICulture)
                {
                    string longDatePattern = CultureInfo.DefaultThreadCurrentUICulture.DateTimeFormat.LongDatePattern;

                    LondDatePatternForCultureNoDayName = new DatePatternForCulture
                    {
                        Culture = CultureInfo.DefaultThreadCurrentUICulture,
                        DatePattern = Regex.Replace(longDatePattern, @"[\s,\.]*dddd[\s,\.]*", "")
                    };
                }

                humanReadableDateString = date.ToString(LondDatePatternForCultureNoDayName.DatePattern, CultureInfo.DefaultThreadCurrentUICulture);
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
