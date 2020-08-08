using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions.Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions
{
    [ClipboxExtension(
    description: "Skips storing of copied words that are highly likely to be passwords (if the word is not part of some text, of course)",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
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

            // At least one letter and one number:
            const string REGEX1 = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]+$";

            // At least one letter, one number and one special character:
            const string REGEX2 = @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]+$";

            // At least one uppercase letter, one lowercase letter and one number:
            const string REGEX3 = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]+$";

            // At least one uppercase letter, one lowercase letter, one number and one special character:
            const string REGEX4 = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$";

            PasswordMatchOptions option = (PasswordMatchOptions)GetSetting<int>(ExtensionSettingNames.PasswordMatchOption);

            // WARNING: NEVER USE THE STATIC METHODS OF THE REGEX CLASS!!!
            // These use A LOT of memory which is never collected.
            //
            // The memory gets allocated the moment before our class is
            // instantiated, as that is the moment the static stuff gets
            // initialized by the CLR.
            //
            // This means that the static regex methods WILL occupy the
            // memory regardless of whether they are actually called.
            //
            // Also, do not use RegexOptions.Compiled, as there are
            // a lot of complications, coming with it, as decribed here:
            // https://blog.codinghorror.com/to-compile-or-not-to-compile/
            Regex rg = null;
            switch (option)
            {
                case PasswordMatchOptions.First:
                    rg = new Regex(REGEX1);
                    break;
                case PasswordMatchOptions.Second:
                    rg = new Regex(REGEX2);
                    break;
                case PasswordMatchOptions.Third:
                    rg = new Regex(REGEX3);
                    break;
                case PasswordMatchOptions.Fourth:
                    rg = new Regex(REGEX4);
                    break;
            }

            return rg?.IsMatch(str) ?? false;
        }

        private int[] GetMinMaxPasswordLen()
        {
            string lenPair = GetSetting<string>(ExtensionSettingNames.PasswordLengthPair);
            
            string[] valueParts = lenPair?.Split(':');

            if (valueParts != null && valueParts.Length == 2)
            {
                return new int[] { int.Parse(valueParts[0]), int.Parse(valueParts[1]) };
            }
            else
            {
                throw new InvalidOperationException("Configuration error.");
            }
        }
    }
}
