using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Clipbox.Infrastructure.ExtensionMethods
{
    public static class EnumExtensions
    {
        public static string GetDescription<T>(this T source) where T : IConvertible
        {
            FieldInfo fi = source
                .GetType()
                .GetField(source.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.
                GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            { 
                return source.ToString();
            }
        }

        public static IEnumerable<string> GetDescriptionsForFlagsEnum(this Enum flagsEnum)
        {
            if (flagsEnum.GetType().
               GetCustomAttributes(typeof(FlagsAttribute), false)
                ?.Any() == true)
            {
                foreach (Enum enumValue in Enum.GetValues(flagsEnum.GetType()))
                {
                    if (flagsEnum.HasFlag(enumValue))
                    {
                        yield return enumValue.GetDescription();
                    }
                }
            }
            else
            {
                throw new ArgumentException($"{nameof(flagsEnum)} is not a flags enum.");
            }
        }
    }
}
