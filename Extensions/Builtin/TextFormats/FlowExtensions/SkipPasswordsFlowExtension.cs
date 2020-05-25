using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions.Common;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions
{
    [ClipboxExtension]
    public class SkipPasswordsFlowExtension :  FlowExtension
    {
        public override void OnItemStoring(IWindowInfo sourceWindowInfo, IEnumerable<IIntermediateFormat> originalIntermediateFormats, ref IList<IIntermediateFormat> modifiedIntermediateFormats, ref bool cancelStoreToItem)
        {
            if (GetSetting<bool>(ExtensionSettingNames.SkipPasswords))
            {
                string text = GetTextFromIntermediateFormat.GetText(originalIntermediateFormats);
                int[] minMaxPasswordLen = GetMinMaxPasswordLen();

                cancelStoreToItem =
                    text != null
                    && text.Length >= minMaxPasswordLen[0]
                    && text.Length <= minMaxPasswordLen[1]
                    && IsStringAPassword(text);
            }
        }

        private bool IsStringAPassword(string str)
        {
            // https://stackoverflow.com/questions/19605150/regex-for-password-must-contain-at-least-eight-characters-at-least-one-number-a

            // Minimum eight characters, at least one letter and one number:
            const string REGEX1 = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]+$";

            // Minimum eight characters, at least one letter, one number and one special character:
            const string REGEX2 = @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]+$";

            // Minimum eight characters, at least one uppercase letter, one lowercase letter and one number:
            const string REGEX3 = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]+$";

            // Minimum eight characters, at least one uppercase letter, one lowercase letter, one number and one special character:
            const string REGEX4 = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$";

            PasswordMatchOptions option = (PasswordMatchOptions)GetSetting<int>(ExtensionSettingNames.PasswordMatchOption);

            switch(option)
            {
                case PasswordMatchOptions.First:
                    return Regex.IsMatch(str, REGEX1, RegexOptions.Compiled);
                case PasswordMatchOptions.Second:
                    return Regex.IsMatch(str, REGEX2, RegexOptions.Compiled);
                case PasswordMatchOptions.Third:
                    return Regex.IsMatch(str, REGEX3, RegexOptions.Compiled);
                case PasswordMatchOptions.Fourth:
                    return Regex.IsMatch(str, REGEX4, RegexOptions.Compiled);
            }

            return false;
        }

        private int[] GetMinMaxPasswordLen()
        {
            const int DEFAULT_MIN_PASSWORD_LEN = 8;
            const int DEFAULT_MAX_PASSWORD_LEN = 20;

            string lenPair = GetSetting<string>(ExtensionSettingNames.PasswordLengthPair);
            if (lenPair != null)
            {
                string[] valueParts = lenPair.Split(':');
                if (valueParts.Length == 2)
                {
                    return new int[] { int.Parse(valueParts[0]), int.Parse(valueParts[1]) };
                }
            }

            return new int[] { DEFAULT_MIN_PASSWORD_LEN, DEFAULT_MAX_PASSWORD_LEN };
        }
    }
}
