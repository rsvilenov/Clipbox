using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Infrastructure.Extensibility.Containers;
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

        public void LoadContainers()
        {
            LoadedTypes = new List<Type>();

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
            
            foreach (string assemblyPath in assemblyPaths)
            {
                if (ClipboxBaseAssemblies.Contains(Path.GetFileName(assemblyPath)))
                {
                    continue;
                }

                var assebmly = Assembly.LoadFrom(assemblyPath);
                AssemblyWalker walker = AssemblyWalker.FromAssembly(assebmly);

                // find all extensions of type IClipboardFormatSupport
                var cfsExtensions = walker.FindExtensions<IClipboardFormatSupport>();
                foreach (var ext in cfsExtensions)
                {
                    FormatsContainer.RegisterExtension(ext);
                    LoadedTypes.Add(ext);
                }

                // find all extensions of type IPreviewHandler
                var previewExtensions = walker.FindExtensions<IPreviewGenerator>();
                foreach (var ext in previewExtensions)
                {
                    PreviewGeneratorsContainer.RegisterExtension(ext);
                    LoadedTypes.Add(ext);
                }

                // find all extensions of type ISearchSupport
                var searchSupportExtensionExtensions = walker.FindExtensions<ISearchSupport>();
                foreach (var ext in searchSupportExtensionExtensions)
                {
                    SearchSupportExtensionContainer.RegisterExtension(ext);
                    LoadedTypes.Add(ext);
                }

                // find all extensions of type IPersistenceSupport
                var persistenceExtensions = walker.FindExtensions<IPersistenceSupport>();
                foreach (var ext in persistenceExtensions)
                {
                    PersistenceSupportExtensionContainer.RegisterExtension(ext);
                    LoadedTypes.Add(ext);
                }

                // find all extensions of type FlowExtension
                var flowExtensions = walker.FindExtensions<FlowExtension>();
                foreach (var ext in flowExtensions)
                {
                    FlowExtensionContainer.RegisterExtension(ext);
                    LoadedTypes.Add(ext);
                }

                // find all extensions of type UIExtension
                var uiExtensions = walker.FindExtensions<UIExtension>();
                foreach (var ext in uiExtensions)
                {
                    UIExtensionContainer.RegisterExtension(ext);
                    LoadedTypes.Add(ext);
                }

                // find all extensions of type SettingsUIExtension
                var settingsUiExtensions = walker.FindExtensions<SettingsUIExtension>();
                foreach (var ext in settingsUiExtensions)
                {
                    SettingsUIExtensionContainer.RegisterExtension(ext);
                    LoadedTypes.Add(ext);
                }
            }
        }
        
        private void Container_ExtensionResolved(object sender, ExtensionEventArgs e)
        {
            StatelessExtensionResolved?.Invoke(this, e);
        }
    }
}
