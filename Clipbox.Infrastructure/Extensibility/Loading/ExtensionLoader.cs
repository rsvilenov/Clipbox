using Clipbox.Core;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.CloudSupportTypes;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Extensibility.Discovery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Clipbox.Infrastructure.Extensibility.Loading
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
        public CloudSupportExtensionContainer CloudSupportExtensionContainer { get; private set; }

        public List<Type> LoadedTypes { get; private set; }
        public List<IExtensionInfo> LoadedExtensions { get; private set; }
        private List<PersistableExtensionInfo> EnabledExtensions { get; set; }

        public EventHandler<ExtensionEventArgs> StatelessExtensionResolved;

        public ExtensionLoader()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AppDomain_ReflectionOnlyAssemblyResolve;

            LoadedTypes = new List<Type>();
            LoadedExtensions = new List<IExtensionInfo>();
            EnabledExtensions = new List<PersistableExtensionInfo>();
        }

        public void SetEnabledExtensions(IEnumerable<PersistableExtensionInfo> enabledExtensionInfo)
        {
            EnabledExtensions.AddRange(enabledExtensionInfo);
        }

        public void LoadContainers()
        {
            if (!EnabledExtensions.Any())
            {
                throw new InvalidOperationException($"No extensions has been enabled yet. Call {nameof(SetEnabledExtensions)} first.");
            }

            if (LoadedExtensions.Any())
            {
                throw new InvalidOperationException("There are already loaded extensions in the containers.");
            }

            FormatsContainer = new ClipboardFormatExtensionContainer();
            PreviewGeneratorsContainer = new PreviewGeneratorExtensionContainer();
            SearchSupportExtensionContainer = new SearchSupportExtensionContainer();
            PersistenceSupportExtensionContainer = new PersistenceSupportExtensionContainer();
            CloudSupportExtensionContainer = new CloudSupportExtensionContainer();

            // the above three containers contain stateless extensions and we can track their loading
            FormatsContainer.ExtensionResolved += (s, e) => StatelessExtensionResolved?.Invoke(this, e);
            PreviewGeneratorsContainer.ExtensionResolved += (s, e) => StatelessExtensionResolved?.Invoke(this, e);
            SearchSupportExtensionContainer.ExtensionResolved += (s, e) => StatelessExtensionResolved?.Invoke(this, e);
            PersistenceSupportExtensionContainer.ExtensionResolved += (s, e) => StatelessExtensionResolved?.Invoke(this, e);

            // these two are stateful and we can only guess when they are used
            FlowExtensionContainer = new FlowExtensionContainer();
            UIExtensionContainer = new UIExtensionContainer();
            SettingsUIExtensionContainer = new SettingsUIExtensionContainer();
            
            // Load the extension assemblies
            string[] assemblyPaths = GetAllExtensionAssemblyPaths();

            // TODO: filter assemblies according to the "EnabledExtensions" setting

            foreach (string assemblyPath in assemblyPaths)
            {
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

                // find all extensions of type CloudSupportExtension
                LoadAndRegisterExtensions<CloudSupportExtension>(CloudSupportExtensionContainer, assemblyPath);
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
            AssemblyWalker fullWalker = null;
            if (_fullWalkersForAssemblyPath.ContainsKey(assemblyPath))
            {
                fullWalker = _fullWalkersForAssemblyPath[assemblyPath];
            }

            // if not, load it in a reflection-only context, in order to prevent any (potentially mallicious) code
            // in a module initializer from running. We will fully load only assemblies containing at least one
            // enabled extension.
            if (fullWalker == null)
            {
                AssemblyWalker reflectionOnlyWalker = GetRefletionOnlyAssemblyWalker(assemblyPath);

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

        public IEnumerable<ExtensionTypeInfo> GetAllAvailableExtensions()
        {
            string[] assemblyPaths = GetAllExtensionAssemblyPaths();

            foreach (string assemblyPath in assemblyPaths)
            {
                AssemblyWalker reflectionOnlyWalker = GetRefletionOnlyAssemblyWalker(assemblyPath);

                // check whether there are any compatible and enabled extensions in this assembly
                var extInfos = reflectionOnlyWalker.FindExtensions<IClipboxExtension>();
                foreach (var extInfo in extInfos)
                {
                    if (IsExtensionVersionCompatible(extInfo))
                    {
                        Assembly assembly = reflectionOnlyWalker.GetAssembly();
                        var persistableExtensionInfo = new PersistableExtensionInfo
                        {
                            AssemblyFullName = assembly.FullName,
                            AssemblyLocation = assembly.Location,
                            Author = extInfo.Author,
                            Description = extInfo.Description,
                            ExtensionObjectTypeFullName = extInfo.ExtensionObjectType.FullName,
                            TypeNameOfExtension = extInfo.ExtensionObjectType.Name,
                            Version = extInfo.Version?.ToString(),
                            MinHostVersion = extInfo.MinHostVersion?.ToString()
                        };

                        yield return new ExtensionTypeInfo(extInfo.ExtensionObjectType, persistableExtensionInfo);
                    }
                }
            }
        }

        private static string[] GetAllExtensionAssemblyPaths()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Directory.GetFiles(assemblyDir, "Clipbox.Extensions.*.dll", SearchOption.AllDirectories);
        }

        private AssemblyWalker GetRefletionOnlyAssemblyWalker(string assemblyPath)
        {
            AssemblyWalker reflectionOnlyWalker = null;
            if (_reflectionOnlyWalkersForAssemblyPath.ContainsKey(assemblyPath))
            {
                reflectionOnlyWalker = _reflectionOnlyWalkersForAssemblyPath[assemblyPath];
            }

            if (reflectionOnlyWalker == null)
            {
                var reflectionOnlyAssembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
                reflectionOnlyWalker = AssemblyWalker.FromAssembly(reflectionOnlyAssembly);
                _reflectionOnlyWalkersForAssemblyPath.Add(assemblyPath, reflectionOnlyWalker);
            }

            return reflectionOnlyWalker;
        }

        private bool IsExtensionVersionCompatible(IExtensionInfo extInfo)
        {
            return extInfo.Version <= ProductConstants.PRODUCT_VERSION;
        }

        private bool IsExtensionEnabled(IExtensionInfo extInfo)
        {
            return EnabledExtensions.Any(pei => pei.ExtensionObjectTypeFullName == extInfo.ExtensionObjectType.FullName
                                            && pei.AssemblyFullName == pei.AssemblyFullName);
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
                return Assembly.ReflectionOnlyLoadFrom(asmPath);
            }
        }

        private void Container_ExtensionResolved(object sender, ExtensionEventArgs e)
        {
            StatelessExtensionResolved?.Invoke(this, e);
        }
    }
}
