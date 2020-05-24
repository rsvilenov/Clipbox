namespace Clipbox.Extensions.BuiltIn.TextFormats
{
    public static class ExtensionSettingNames
    {
        // preview generation extensions
        public static string FontScaleFactor = nameof(FontScaleFactor);
        public static string ScaleFontSizeToDpi = nameof(ScaleFontSizeToDpi);
        public static string PreserveOriginalFontProperties = nameof(PreserveOriginalFontProperties);
        public static string PreferredBackground = nameof(PreferredBackground);
        public static string PreferredForeground = nameof(PreferredForeground);
        public static string FormattingCharsForeground = nameof(FormattingCharsForeground);
        public static string IncludeAnnotation = nameof(IncludeAnnotation);
        public static string Compactify = nameof(Compactify);
        public static string StretchBackgroundToFill = nameof(StretchBackgroundToFill);

        // extensions for pasted text formatting
        public static string DefaultSeparator = nameof(DefaultSeparator);
        public static string SaveSelectedSeparatorAsDefault = nameof(SaveSelectedSeparatorAsDefault);
        public static string RemoveFormatting = nameof(RemoveFormatting);
        public static string SaveRemoveFormattingState = nameof(SaveRemoveFormattingState);

        // extensions for on copy modifications of text formats
        public static string MinTextLengthToStore = nameof(MinTextLengthToStore);
        public static string SkipMaskCreditCardNumbers = nameof(SkipMaskCreditCardNumbers);
        public static string SkipPasswords = nameof(SkipPasswords);
        public static string PasswordMinLength = nameof(PasswordMinLength);
        public static string PasswordMaxLength = nameof(PasswordMaxLength);
        public static string PasswordMatchOption = nameof(PasswordMatchOption);
    }
}
