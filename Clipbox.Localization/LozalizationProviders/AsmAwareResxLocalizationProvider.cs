using Clipbox.Infrastructure.UI.Localization.Enums;
using Clipbox.Infrastructure.UI.Localization.EventArgsClasses;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace Clipbox.Infrastructure.UI.Localization.LozalizationProviders
{
    public class AsmAwareResxLocalizationProvider : IAssemblyAwareLocalizationProvider
    {
        private readonly ConcurrentDictionary<string, List<Type>> _resourceClasses;

        private readonly ConcurrentDictionary<string, List<ResourceManager>> _liveResourceManagers;

        static AsmAwareResxLocalizationProvider()
        {
            Instance = new AsmAwareResxLocalizationProvider();
        }

        private AsmAwareResxLocalizationProvider()
        {
            _resourceClasses = new ConcurrentDictionary<string, List<Type>>();
            _liveResourceManagers = new ConcurrentDictionary<string, List<ResourceManager>>();

            LoadResources();

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }


        public static AsmAwareResxLocalizationProvider Instance { get; }

        private CultureInfo _currentCulture;

        public CultureInfo CurrentCulture
        {
            get { return _currentCulture; }
            set
            {
                if (_currentCulture != value)
                {
                    CultureInfo oldCulture = _currentCulture;
                    _currentCulture = value;
                    LocalizationCultureChanged?.Invoke(this, new CultureChangedEventArgs(oldCulture, _currentCulture));
                }
            }
        }

        public event EventHandler<CultureChangedEventArgs> LocalizationCultureChanged;

        
        private bool _keepLiveResourceManagers;

        public bool KeepLiveResourceManagers
        {
            get
            {
                return _keepLiveResourceManagers;
            }
            set
            {
                if (_keepLiveResourceManagers != value)
                {
                    _keepLiveResourceManagers = value;

                    if (_keepLiveResourceManagers == false)
                    {
                        _liveResourceManagers.Clear();
                    }
                }
            }
        }
        
        public void ReleaseLiveManagers(Assembly asm)
        {
            _liveResourceManagers.TryRemove(asm.FullName, out List<ResourceManager> _);
        }

        public string GetTranslation(string key)
        {
            return GetTranslation(key, ResourceLookupMode.Everywhere, null);
        }

        public string GetTranslation(string key, ResourceLookupMode mode, Assembly asm)
        {
            IEnumerable<ResourceManager> managers;

            if (asm != null
                && mode == ResourceLookupMode.CurrentAssembly)
            {
                
                managers = _liveResourceManagers
                    .Where(k => k.Key == asm.FullName)
                    .SelectMany(k => k.Value);

                if (!managers.Any())
                {
                    managers = _resourceClasses
                    .Where(k => k.Key == asm.FullName)
                    .SelectMany(k => GetResourceManagers(k.Value));

                    if (KeepLiveResourceManagers)
                    {
                        _liveResourceManagers.TryAdd(asm.FullName, managers.ToList());
                    }
                }
            }
            else
            {
                managers = _liveResourceManagers
                    .SelectMany(k => k.Value);

                if (!managers.Any())
                {
                    managers = _resourceClasses
                    .SelectMany(k => GetResourceManagers(k.Value));

                    if (KeepLiveResourceManagers)
                    {
                        _liveResourceManagers.TryAdd(asm.FullName, managers.ToList());
                    }
                }
            }

            return GetTranslation(key, managers);
        }

        private string GetTranslation(string key, IEnumerable<ResourceManager> managers)
        {
            foreach (ResourceManager resMan in managers)
            {
                if (resMan.GetString(key, this.CurrentCulture) is string val)
                {
                    return val;
                }
            }

            return string.Empty;
        }
        
        private void LoadResources()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                CacheResourceManagers(asm);
            }
        }

        private void CacheResourceManagers(Assembly asm)
        {
            List<Type> resManagerTypes = GetResourceClasses(asm);
            if (!_resourceClasses.ContainsKey(asm.FullName))
            {
                _resourceClasses.TryAdd(asm.FullName, resManagerTypes);
                if (KeepLiveResourceManagers)
                {
                    _liveResourceManagers.TryAdd(asm.FullName, GetResourceManagers(resManagerTypes));
                }
            }
        }
    
        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            CacheResourceManagers(args.LoadedAssembly);
        }

        private List<Type> GetResourceClasses(Assembly asm)
        {
            return asm
                .GetTypes()
                .Where(t => t.GetProperty("ResourceManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null)
                .ToList();
        }

        private List<ResourceManager> GetResourceManagers(List<Type> resourceManagerTypes)
        {
            return resourceManagerTypes
                .Select(t => t.GetProperty("ResourceManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null) as ResourceManager)
                .ToList();
        }
    }
}
