using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Clipbox.ViewModels
{
    public static class AppDescriptionFormatter
    {
        private const string DESCRIPTION_FORMATS_FILEPATH = "Resources\\Formats\\ApplicationDescriptionFormats.xml";
        private static List<AppDescriptionFormatInfo> _appDescriptionFormatInfos = new List<AppDescriptionFormatInfo>();

        static AppDescriptionFormatter()
        {
            LoadAppDescriptionFormatInfos();
        }

        public static string Format(IWindowInfo windowInfo)
        {
            AppDescriptionFormatInfo matchedInfo = _appDescriptionFormatInfos
                .FirstOrDefault(adfi => adfi.Match(windowInfo.ApplicationInfo)) ??
                _appDescriptionFormatInfos.FirstOrDefault(adfi => adfi.IsDefault);

            if (matchedInfo == null)
            {
                throw new InvalidOperationException($"No item in {nameof(_appDescriptionFormatInfos)} matched {windowInfo.ApplicationInfo.ApplicationPath}");
            }

            return GetFormattedDescription(matchedInfo.WindowInfoFormat, windowInfo.ApplicationInfo, windowInfo);
        }

        public static string Format(IApplicationInfo appInfo)
        {
            AppDescriptionFormatInfo matchedInfo = _appDescriptionFormatInfos
                .FirstOrDefault(adfi => adfi.Match(appInfo)) ??
                _appDescriptionFormatInfos.FirstOrDefault(adfi => adfi.IsDefault);

            if (matchedInfo == null)
            {
                throw new InvalidOperationException($"No item in {nameof(_appDescriptionFormatInfos)} matched {appInfo.ApplicationPath}");
            }

            return GetFormattedDescription(matchedInfo.ApplicationInfoFormat, appInfo, null);
        }

        private static void LoadAppDescriptionFormatInfos()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DESCRIPTION_FORMATS_FILEPATH));
            XmlElement root = doc.DocumentElement;
            XmlNodeList nodes = root.SelectNodes("application");
            foreach (XmlNode node in nodes)
            {
                _appDescriptionFormatInfos.Add(new AppDescriptionFormatInfo
                {
                    IsDefault = node.Attributes.Count == 0,
                    FileDescriptionMatchPattern = node.Attributes["FileDescriptionMatchPattern"]?.Value,
                    ApplicationPathMatchPattern = node.Attributes["ApplicationPathMatchPattern"]?.Value,
                    ProductNameMatchPattern = node.Attributes["ProductNameMatchPattern"]?.Value,
                    ProductVersionMatchPattern = node.Attributes["ProductVersionMatchPattern"]?.Value,
                    CompanyNameMatchPattern = node.Attributes["CompanyNameMatchPattern"]?.Value,

                    WindowInfoFormat = node.SelectSingleNode("windowInfoFormat")?.InnerText,
                    ApplicationInfoFormat = node.SelectSingleNode("applicationInfoFormat")?.InnerText
                });
            }
        }

        private class AppDescriptionFormatInfo
        {
            public string FileDescriptionMatchPattern { get; set; }
            public string ApplicationPathMatchPattern { get; set; }
            public string ProductNameMatchPattern { get; set; }
            public string ProductVersionMatchPattern { get; set; }
            public string CompanyNameMatchPattern { get; set; }

            public bool IsDefault { get; set; }

            public string WindowInfoFormat { get; set; }
            public string ApplicationInfoFormat { get; set; }
            
            
            public bool Match(IApplicationInfo appInfo)
            {
                // this is ugly, but faster than regex 

                if (!string.IsNullOrEmpty(FileDescriptionMatchPattern) && !string.IsNullOrEmpty(appInfo.FileDescription))
                {
                    return FileDescriptionMatchPattern.Equals(appInfo.FileDescription, StringComparison.OrdinalIgnoreCase) ||
                        (FileDescriptionMatchPattern[0] == '*' && FileDescriptionMatchPattern[FileDescriptionMatchPattern.Length - 1] == '*'
                        && appInfo.FileDescription.IndexOf(FileDescriptionMatchPattern, StringComparison.OrdinalIgnoreCase) > -1) ||

                        (FileDescriptionMatchPattern[0] == '*' && FileDescriptionMatchPattern.EndsWith(appInfo.FileDescription, StringComparison.OrdinalIgnoreCase)) ||

                        (FileDescriptionMatchPattern[FileDescriptionMatchPattern.Length - 1] == '*' && FileDescriptionMatchPattern.StartsWith(appInfo.FileDescription, StringComparison.OrdinalIgnoreCase));
                }
                else if (!string.IsNullOrEmpty(ApplicationPathMatchPattern) && !string.IsNullOrEmpty(appInfo.ApplicationPath))
                {
                    return ApplicationPathMatchPattern.Equals(appInfo.ApplicationPath, StringComparison.OrdinalIgnoreCase) ||
                        (ApplicationPathMatchPattern[0] == '*' && ApplicationPathMatchPattern[ApplicationPathMatchPattern.Length - 1] == '*'
                        && appInfo.ApplicationPath.IndexOf(ApplicationPathMatchPattern, StringComparison.OrdinalIgnoreCase) > -1) ||

                        (ApplicationPathMatchPattern[0] == '*' && ApplicationPathMatchPattern.EndsWith(appInfo.ApplicationPath, StringComparison.OrdinalIgnoreCase)) ||

                        (ApplicationPathMatchPattern[ApplicationPathMatchPattern.Length - 1] == '*' && ApplicationPathMatchPattern.StartsWith(appInfo.ApplicationPath, StringComparison.OrdinalIgnoreCase));
                }
                else if (!string.IsNullOrEmpty(ProductNameMatchPattern) && !string.IsNullOrEmpty(appInfo.ProductName))
                {
                    return ProductNameMatchPattern.Equals(appInfo.ProductName, StringComparison.OrdinalIgnoreCase) ||
                        (ProductNameMatchPattern[0] == '*' && ProductNameMatchPattern[ProductNameMatchPattern.Length - 1] == '*'
                        && appInfo.ProductName.IndexOf(ProductNameMatchPattern, StringComparison.OrdinalIgnoreCase) > -1) ||

                        (ProductNameMatchPattern[0] == '*' && ProductNameMatchPattern.EndsWith(appInfo.ProductName, StringComparison.OrdinalIgnoreCase)) ||

                        (ProductNameMatchPattern[ProductNameMatchPattern.Length - 1] == '*' && ProductNameMatchPattern.StartsWith(appInfo.ProductName, StringComparison.OrdinalIgnoreCase));
                }
                else if (!string.IsNullOrEmpty(ProductVersionMatchPattern) && !string.IsNullOrEmpty(appInfo.ProductVersion))
                {
                    return ProductVersionMatchPattern.Equals(appInfo.ProductVersion, StringComparison.OrdinalIgnoreCase) ||
                        (ProductVersionMatchPattern[0] == '*' && ProductVersionMatchPattern[ProductVersionMatchPattern.Length - 1] == '*'
                        && appInfo.ProductVersion.IndexOf(ProductVersionMatchPattern, StringComparison.OrdinalIgnoreCase) > -1) ||

                        (ProductVersionMatchPattern[0] == '*' && ProductVersionMatchPattern.EndsWith(appInfo.ProductVersion, StringComparison.OrdinalIgnoreCase)) ||

                        (ProductVersionMatchPattern[ProductVersionMatchPattern.Length - 1] == '*' && ProductVersionMatchPattern.StartsWith(appInfo.ProductVersion, StringComparison.OrdinalIgnoreCase));
                }
                else if (!string.IsNullOrEmpty(CompanyNameMatchPattern) && !string.IsNullOrEmpty(appInfo.CompanyName))
                {
                    return CompanyNameMatchPattern.Equals(appInfo.CompanyName, StringComparison.OrdinalIgnoreCase) ||
                        (CompanyNameMatchPattern[0] == '*' && CompanyNameMatchPattern[CompanyNameMatchPattern.Length - 1] == '*'
                        && appInfo.CompanyName.IndexOf(CompanyNameMatchPattern, StringComparison.OrdinalIgnoreCase) > -1) ||

                        (CompanyNameMatchPattern[0] == '*' && CompanyNameMatchPattern.EndsWith(appInfo.CompanyName, StringComparison.OrdinalIgnoreCase)) ||

                        (CompanyNameMatchPattern[CompanyNameMatchPattern.Length - 1] == '*' && CompanyNameMatchPattern.StartsWith(appInfo.CompanyName, StringComparison.OrdinalIgnoreCase));
                }

                return false;
            }
        }

        // this should be fast enough
        private static string GetFormattedDescription(string format, IApplicationInfo appInfo, IWindowInfo windowInfo = null)
        {
            const int KEY_NAME_LEN_SUM = 150; // the total length of all possible property names (IApplicationInfo.* and IWindowInfo.Caption)

            bool bracketOpen = false;
            char[] propNames = new char[KEY_NAME_LEN_SUM];
            int propNameIndex = 0;
            StringBuilder sb = new StringBuilder();

            foreach (char c in format)
            {
                if (c == '{')
                {
                    bracketOpen = true;
                }
                else if (bracketOpen)
                {
                    if (c == '}')
                    {
                        bracketOpen = false;
                        foreach (string propName in new string(propNames, 0, propNameIndex).Split('|'))
                        {
                            bool isContentForPropNameFilled = false;
                            switch (propName)
                            {
                                case nameof(appInfo.ProductName):
                                    if (!string.IsNullOrWhiteSpace(appInfo.ProductName))
                                    {
                                        sb.Append(appInfo.ProductName);
                                        isContentForPropNameFilled = true;
                                    }
                                    break;
                                case nameof(appInfo.ProductVersion):
                                    if (!string.IsNullOrWhiteSpace(appInfo.ProductVersion))
                                    {
                                        sb.Append(appInfo.ProductVersion);
                                        isContentForPropNameFilled = true;
                                    }
                                    break;
                                case nameof(appInfo.CompanyName):
                                    if (!string.IsNullOrWhiteSpace(appInfo.CompanyName))
                                    {
                                        sb.Append(appInfo.CompanyName);
                                        isContentForPropNameFilled = true;
                                    }
                                    break;
                                case nameof(appInfo.ApplicationPath):
                                    if (!string.IsNullOrWhiteSpace(appInfo.ApplicationPath))
                                    {
                                        sb.Append(appInfo.ApplicationPath);
                                        isContentForPropNameFilled = true;
                                    }
                                    break;
                                case nameof(appInfo.FileDescription):
                                    if (!string.IsNullOrWhiteSpace(appInfo.FileDescription))
                                    {
                                        sb.Append(appInfo.FileDescription);
                                        isContentForPropNameFilled = true;
                                    }
                                    break;
                                case nameof(windowInfo.Caption):
                                    if (!string.IsNullOrWhiteSpace(windowInfo.Caption))
                                    {
                                        sb.Append(windowInfo?.Caption ?? throw new FormatException($"{nameof(windowInfo.Caption)} is used in the format, but no {nameof(IWindowInfo)} was passed."));
                                        isContentForPropNameFilled = true;
                                    }
                                    break;
                            }

                            if (isContentForPropNameFilled)
                            {
                                break;
                            }
                        }
                        propNameIndex = 0;
                        Array.Clear(propNames, 0, KEY_NAME_LEN_SUM);
                    }
                    else
                    {
                        propNames[propNameIndex++] = c;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
