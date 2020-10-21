using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Clipbox.WixSetup.View
{
    public class LocalizationHelper
    {
        private static LocalizationHelper _instance;
        private readonly Dictionary<string, ResourceDictionary> _resourceDictionaries;
        private string currentLangCode;
        public static LocalizationHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LocalizationHelper();
                }

                return _instance;
            }
        }

        public void SetLanguage(string langCode)
        {
            if (_resourceDictionaries.ContainsKey(langCode))
            {
                if (_resourceDictionaries.ContainsKey(currentLangCode)
                    && Application.Current.Resources.MergedDictionaries.Contains(_resourceDictionaries[currentLangCode]))
                {
                    Application.Current.Resources.MergedDictionaries.Remove(_resourceDictionaries[currentLangCode]);
                }
                Application.Current.Resources.MergedDictionaries.Add(_resourceDictionaries[langCode]);
                currentLangCode = langCode;
            }
        }

        private LocalizationHelper()
        {
            const string translationXamlPrefix = "Translations_";
            currentLangCode = "en-US";
            _resourceDictionaries = new Dictionary<string, ResourceDictionary>();

            Assembly myAsm = Assembly
                .GetExecutingAssembly();

            foreach (string translationDicName in
                myAsm.GetManifestResourceNames())
            {
                if (translationDicName.Contains($".{translationXamlPrefix}"))
                {
                    ManifestResourceInfo info = myAsm.GetManifestResourceInfo(translationDicName);
                    if (info.ResourceLocation != ResourceLocation.ContainedInAnotherAssembly)
                    {
                        Stream resourceStream = myAsm.GetManifestResourceStream(translationDicName);
                        if (System.Windows.Markup.XamlReader.Load(resourceStream) is ResourceDictionary resDic)
                        {
                            string cultureCode = "en-US";
                            int startIdx = translationDicName.IndexOf(translationXamlPrefix) + translationXamlPrefix.Length;
                            int endIdx = translationDicName.IndexOf(".xaml");

                            if (startIdx > -1)
                            {
                                cultureCode = translationDicName.Substring(startIdx, endIdx - startIdx);
                            }

                            _resourceDictionaries.Add(cultureCode, resDic);
                        }

                    }
                }
            }
        }
    }
}

