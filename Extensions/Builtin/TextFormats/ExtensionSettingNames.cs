namespace Clipbox.Extensions.BuiltIn.TextFormats
{
    public static class ExtensionSettingNames
    {
        // preview generation extensions
        public static string FontSize = nameof(FontSize);
        public static string ScaleFontSizeToDpi = nameof(ScaleFontSizeToDpi);
        public static string PreserveOriginalFontProperties = nameof(PreserveOriginalFontProperties);
        public static string Background = nameof(Background);
        public static string Foreground = nameof(Foreground);
        public static string ShowFormattingCharacters = nameof(ShowFormattingCharacters);
        public static string IncludeAnnotation = nameof(IncludeAnnotation);
        public static string Compactify = nameof(Compactify);

        // extensions for pasted text formatting
        public static string DefaultSeparator = nameof(DefaultSeparator);
        public static string SaveSelectedSeparatorAsDefault = nameof(SaveSelectedSeparatorAsDefault);
        public static string RemoveFormatting = nameof(RemoveFormatting);
        public static string SaveRemoveFormattingState = nameof(SaveRemoveFormattingState);
    }
}
