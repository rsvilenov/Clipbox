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

                cancelStoreToItem =
                    text != null
                    && text.Length >= GetSetting<int>(ExtensionSettingNames.PasswordMinLength)
                    && text.Length <= GetSetting<int>(ExtensionSettingNames.PasswordMaxLength)
                    && IsStringAPassword(text);
            }
        }

        private bool IsStringAPassword(string str)
        {
            // https://stackoverflow.com/questions/19605150/regex-for-password-must-contain-at-least-eight-characters-at-least-one-number-a

            // Minimum eight characters, at least one letter and one number:
            const string REGEX1 = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$";

            // Minimum eight characters, at least one letter, one number and one special character:
            const string REGEX2 = @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$";

            // Minimum eight characters, at least one uppercase letter, one lowercase letter and one number:
            const string REGEX3 = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{8,}$";

            // Minimum eight characters, at least one uppercase letter, one lowercase letter, one number and one special character:
            const string REGEX4 = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

            PasswordMatchOptions option = (PasswordMatchOptions)GetSetting<int>(ExtensionSettingNames.PasswordMatchOption);

            if (option.HasFlag(PasswordMatchOptions.First) 
                && Regex.IsMatch(REGEX1, str, RegexOptions.Compiled))
            {
                return true;
            }

            if (option.HasFlag(PasswordMatchOptions.Second)
                && Regex.IsMatch(REGEX2, str, RegexOptions.Compiled))
            {
                return true;
            }

            
            if (option.HasFlag(PasswordMatchOptions.Third)
                && Regex.IsMatch(REGEX3, str, RegexOptions.Compiled))
            {
                return true;
            }

            
            if (option.HasFlag(PasswordMatchOptions.Fourth)
                && Regex.IsMatch(REGEX4, str, RegexOptions.Compiled))
            {
                return true;
            }

            return false;
        }
    }
}
