using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Linq;
using System.Windows;

namespace Clipbox.WixSetup.View
{
    public class LocalizationHelper
    {
        private readonly Dictionary<string, ResourceDictionary> _resourceDictionaries;
        private readonly Window _currentWindow;

        private string _currentLangCode;

        public LocalizationHelper(Window wnd)
        {
            _currentWindow = wnd;

            _resourceDictionaries = new Dictionary<string, ResourceDictionary>();
            LoadTranslationResourceDictionaries();
        }

        public void SetLanguage(string langCode)
        {
            if (_resourceDictionaries.ContainsKey(langCode))
            {
                if (_resourceDictionaries.ContainsKey(_currentLangCode)
                    && _currentWindow.Resources.MergedDictionaries.Contains(_resourceDictionaries[_currentLangCode]))
                {
                    _currentWindow.Resources.MergedDictionaries.Remove(_resourceDictionaries[_currentLangCode]);
                }
                _currentWindow.Resources.MergedDictionaries.Add(_resourceDictionaries[langCode]);
                _currentLangCode = langCode;
            }
        }

        private void LoadTranslationResourceDictionaries()
        {
            const string translationXamlPrefix = "Translations_";
            _currentLangCode = "en-US";

            Assembly myAsm = Assembly
                .GetExecutingAssembly();

            foreach (string resName in
                myAsm.GetManifestResourceNames())
            {
                if (resName.Contains($".{translationXamlPrefix}"))
                {
                    ManifestResourceInfo info = myAsm.GetManifestResourceInfo(resName);
                    if (info.ResourceLocation != ResourceLocation.ContainedInAnotherAssembly)
                    {
                        using (Stream resourceStream = myAsm.GetManifestResourceStream(resName))
                        {
                            if (System.Windows.Markup.XamlReader.Load(resourceStream) is ResourceDictionary resDic)
                            {
                                string cultureCode = "en-US";
                                int prefixStartIdx = resName.IndexOf(translationXamlPrefix);
                                if (prefixStartIdx == -1)
                                {
                                    prefixStartIdx = 0;
                                }
                                int startIdx = prefixStartIdx + translationXamlPrefix.Length;
                                int endIdx = resName.IndexOf(".xaml");

                                if (startIdx > -1)
                                {
                                    cultureCode = resName.Substring(startIdx, endIdx - startIdx);
                                }

                                _resourceDictionaries.Add(cultureCode, resDic);
                            }
                        }
                    }
                }
                else if (resName.EndsWith("g.resources"))
                {
                    using (Stream resourceStream = myAsm.GetManifestResourceStream(resName))
                    using (ResourceReader reader = new ResourceReader(resourceStream))
                    {
                        foreach (DictionaryEntry entry in reader)
                        {
                            if (entry.Key.ToString().IndexOf($"/{translationXamlPrefix}", StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                string pathToResDic = entry.Key.ToString().Replace(".baml", ".xaml");
                                if (Application.LoadComponent(new Uri(pathToResDic, UriKind.Relative)) is ResourceDictionary resDic)
                                {
                                    string xamlFileName = pathToResDic.Substring(pathToResDic.LastIndexOf('/') + 1);
                                    string cultureCode = "en-US";
                                    int prefixStartIdx = xamlFileName.IndexOf(translationXamlPrefix);
                                    if (prefixStartIdx == -1)
                                    {
                                        prefixStartIdx = 0;
                                    }
                                    int startIdx = prefixStartIdx + translationXamlPrefix.Length;
                                    int endIdx = xamlFileName.IndexOf(".xaml");

                                    if (startIdx > -1)
                                    {
                                        cultureCode = xamlFileName.Substring(startIdx, endIdx - startIdx);
                                        string[] codeParts = cultureCode.Split('-');
                                        if (codeParts.Length != 2)
                                        {
                                            continue;
                                        }

                                        if (!codeParts[1].All(c => char.IsUpper(c)))
                                        {
                                            cultureCode = $"{codeParts[0]}-{codeParts[1].ToUpperInvariant()}";
                                        }
                                    }

                                    _resourceDictionaries.Add(cultureCode, resDic);
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}

