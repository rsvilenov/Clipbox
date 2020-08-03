using Clipbox.Core;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Extensibility.Discovery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Clipbox
{
    public class ExtensionLoader
    {
        public ClipboardFormatExtensionContainer FormatsContainer { get; private set; }

        public PreviewGeneratorExtensionContainer PreviewGeneratorsContainer { get; private set; }

        public SearchSupportExtensionContainer SearchSupportExtensionContainer { get; private set; }

        public FlowExtensionContainer FlowExtensionContainer { get; private set; }

        public UIExtensionContainer UIExtensionContainer { get; private set; }

        public SettingsUIExtensionContainer SettingsUIExtensionContainer { get; private set; }

        public PersistenceSupportExtensionContainer PersistenceSupportExtensionContainer { get; private set; }

        public List<Type> LoadedTypes { get; private set; }

        public EventHandler<ExtensionEventArgs> StatelessExtensionResolved;

        public List<IExtensionInfo> LoadedExtensions { get; private set; }

        public void LoadContainers()
        {
            LoadedTypes = new List<Type>();
            LoadedExtensions = new List<IExtensionInfo>();

            FormatsContainer = new ClipboardFormatExtensionContainer();
            PreviewGeneratorsContainer = new PreviewGeneratorExtensionContainer();
            SearchSupportExtensionContainer = new SearchSupportExtensionContainer();
            PersistenceSupportExtensionContainer = new PersistenceSupportExtensionContainer();

            // the above three containers contain stateless extensions and we can track their loading
            FormatsContainer.ExtensionResolved += (s, e) => StatelessExtensionResolved?.Invoke(this, e);
            PreviewGeneratorsContainer.ExtensionResolved += (s, e) => StatelessExtensionResolved?.Invoke(this, e);
            SearchSupportExtensionContainer.ExtensionResolved += (s, e) => StatelessExtensionResolved?.Invoke(this, e);
            PersistenceSupportExtensionContainer.ExtensionResolved += (s, e) => StatelessExtensionResolved?.Invoke(this, e);

            // these two are stateful and we can only guess when they are used
            FlowExtensionContainer = new FlowExtensionContainer();
            UIExtensionContainer = new UIExtensionContainer();
            SettingsUIExtensionContainer = new SettingsUIExtensionContainer();
            
            // do not search for extensions in these assemblies
            string[] ClipboxBaseAssemblies =
            {
                "Clipbox.Infrastructure.dll",
                "Clipbox.Infrastructure.UI.dll",
                "Clipbox.Infrastructure.UI.CustomControls.dll",
                "Clipbox.Win32.dll",
                "Clipbox.ExtendedClipboard.dll",
                "Clipbox.Core.dll",
                "Clipbox.SearchProvider.dll",
                "Clipbox.CloudServices.dll"
            };

            // Load the extension assemblies
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] assemblyPaths = Directory.GetFiles(assemblyDir, "Clipbox.Extensions.*.dll", SearchOption.AllDirectories);

            // TODO: filter assemblies according to the "EnabledExtensions" setting

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AppDomain_ReflectionOnlyAssemblyResolve;

            foreach (string assemblyPath in assemblyPaths)
            {
                if (ClipboxBaseAssemblies.Contains(Path.GetFileName(assemblyPath)))
                {
                    continue;
                }

                // find all extensions of type IClipboardFormatSupport
                LoadAndRegisterExtensions<IClipboardFormatSupport>(FormatsContainer, assemblyPath);
              
                // find all extensions of type IPreviewHandler
                LoadAndRegisterExtensions<IPreviewGenerator>(PreviewGeneratorsContainer, assemblyPath);
                
                // find all extensions of type ISearchSupport
                LoadAndRegisterExtensions<ISearchSupport>(SearchSupportExtensionContainer, assemblyPath);

                // find all extensions of type IPersistenceSupport
                LoadAndRegisterExtensions<IPersistenceSupport>(PersistenceSupportExtensionContainer, assemblyPath);

                // find all extensions of type FlowExtension
                LoadAndRegisterExtensions<FlowExtension>(FlowExtensionContainer, assemblyPath);

                // find all extensions of type UIExtension
                LoadAndRegisterExtensions<IFlowUIExtension>(UIExtensionContainer, assemblyPath);
                
                // find all extensions of type SettingsUIExtension
                LoadAndRegisterExtensions<SettingsUIExtension>(SettingsUIExtensionContainer, assemblyPath);
            }
        }

        private Dictionary<string, AssemblyWalker> _fullWalkersForAssemblyPath = new Dictionary<string, AssemblyWalker>();
        private Dictionary<string, AssemblyWalker> _reflectionOnlyWalkersForAssemblyPath = new Dictionary<string, AssemblyWalker>();

        private void LoadAndRegisterExtensions<TExtensionBaseType>(IExtensionRegistrator extRegistrator, string assemblyPath)
            where TExtensionBaseType : IClipboxExtension
        {
            // if the assembly does not contain compatible and enabled extensions, the method below returns null
            AssemblyWalker fullWalker = GetFullWalkerForCompatibleAndEnabledAssembly(assemblyPath);

            if (fullWalker != null)
            {
                var extInfos = fullWalker.FindExtensions<TExtensionBaseType>();
                foreach (var extInfo in extInfos)
                {
                    if (IsExtensionVersionCompatible(extInfo) && IsExtensionEnabled(extInfo))
                    {
                        extRegistrator.RegisterExtension(extInfo.ExtensionObjectType);
                        LoadedExtensions.Add(extInfo);
                    }
                }
            }
        }

        private AssemblyWalker GetFullWalkerForCompatibleAndEnabledAssembly(string assemblyPath)
        {
            // check whether the assembly has been fully loaded already
            AssemblyWalker fullWalker = _fullWalkersForAssemblyPath
                .Where(w => w.Key == assemblyPath)
                .Select(w => w.Value)
                .FirstOrDefault();

            // if not, load it in a reflection-only context
            if (fullWalker == null)
            {
                AssemblyWalker reflectionOnlyWalker = _reflectionOnlyWalkersForAssemblyPath
                    .Where(w => w.Key == assemblyPath)
                    .Select(w => w.Value)
                    .FirstOrDefault();

                if (reflectionOnlyWalker == null)
                {
                    var eflectionOnlyAssembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
                    reflectionOnlyWalker = AssemblyWalker.FromAssembly(eflectionOnlyAssembly);
                    _reflectionOnlyWalkersForAssemblyPath.Add(assemblyPath, reflectionOnlyWalker);
                }

                // check whether there are any compatible and enabled extensions in this assembly
                var extInfos = reflectionOnlyWalker.FindExtensions<IClipboxExtension>();
                foreach (var extInfo in extInfos)
                {
                    if (IsExtensionVersionCompatible(extInfo) && IsExtensionEnabled(extInfo))
                    {
                        // if there are compatible extensions, load the assembly fully and cache its assembly walker
                        var fullyLoadedAssembly = Assembly.LoadFrom(extInfo.ExtensionObjectType.Assembly.Location);
                        fullWalker = AssemblyWalker.FromAssembly(fullyLoadedAssembly);
                        _fullWalkersForAssemblyPath.Add(assemblyPath, fullWalker);
                        break;
                    }
                }
            }

            return fullWalker;
        }

        private bool IsExtensionVersionCompatible(IExtensionInfo extInfo)
        {
            return extInfo.Version <= ProductConstants.PRODUCT_VERSION;
        }

        private bool IsExtensionEnabled(IExtensionInfo extInfo)
        {
            return true;
        }

        private Assembly AppDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                return Assembly.ReflectionOnlyLoad(args.Name);
            }
            catch(FileNotFoundException)
            {
                string asmPath = Path.Combine(Path.GetDirectoryName(args.RequestingAssembly.Location), args.Name);
                return Assembly.ReflectionOnlyLoad(args.Name);
            }
        }

        private void Container_ExtensionResolved(object sender, ExtensionEventArgs e)
        {
            StatelessExtensionResolved?.Invoke(this, e);
        }
    }
}
