using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.Extensibility.Discovery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Clipbox
{
    public class ExtentionLoader
    {
        public ClipboardFormatExtentionContainer FormatsContainer { get; private set; }

        public ClipboardFormatPreviewExtentionContainer PreviewGeneratorsContainer { get; private set; }

        public SearchSupportExtensionContainer SearchSupportExtensionContainer { get; private set; }

        public FlowExtentionContainer FlowExtentionContainer { get; private set; }

        public UIExtentionContainer UIExtentionContainer { get; private set; }
        
        public List<Type> LoadedTypes { get; private set; }

        public EventHandler<ExtentionEventArgs> StatelessExtentionResolved;

        public void LoadContainers()
        {
            LoadedTypes = new List<Type>();

            FormatsContainer = new ClipboardFormatExtentionContainer();
            PreviewGeneratorsContainer = new ClipboardFormatPreviewExtentionContainer();
            SearchSupportExtensionContainer = new SearchSupportExtensionContainer();

            // the above three containers contain stateless extentions and we can track their loading
            FormatsContainer.ExtentionResolved += (s, e) => StatelessExtentionResolved?.Invoke(this, e);
            PreviewGeneratorsContainer.ExtentionResolved += (s, e) => StatelessExtentionResolved?.Invoke(this, e);
            SearchSupportExtensionContainer.ExtentionResolved += (s, e) => StatelessExtentionResolved?.Invoke(this, e);

            // these two are stateful and we can only guess when they are used
            FlowExtentionContainer = new FlowExtentionContainer();
            UIExtentionContainer = new UIExtentionContainer();
            
            // do not search for extentions in these assemblies
            string[] ClipboxBaseAssemblies =
            {
                "Clipbox.Infrastructure.dll",
                "Clipbox.Win32.dll",
                "Clipbox.ExtendedClipboard.dll"
            };

            // Load the extention assemblies
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] assemblyPaths = Directory.GetFiles(assemblyDir, "Clipbox.Extentions.*.dll", SearchOption.AllDirectories);
            
            foreach (string assemblyPath in assemblyPaths)
            {
                if (ClipboxBaseAssemblies.Contains(Path.GetFileName(assemblyPath)))
                {
                    continue;
                }

                var assebmly = Assembly.LoadFrom(assemblyPath);
                AssemblyWalker walker = AssemblyWalker.FromAssembly(assebmly);

                // find all extentions of type IClipboardFormatSupport
                var cfsExtentions = walker.FindExtentions<IClipboardFormatSupport>();
                foreach (var ext in cfsExtentions)
                {
                    FormatsContainer.RegisterExtention(ext);
                    LoadedTypes.Add(ext);
                }

                // find all extentions of type IPreviewHandler
                var previewExtentions = walker.FindExtentions<IPreviewHandler>();
                foreach (var ext in previewExtentions)
                {
                    PreviewGeneratorsContainer.RegisterExtention(ext);
                    LoadedTypes.Add(ext);
                }

                // find all extentions of type SearchSupportExtension
                var searchSupportExtensionExtentions = walker.FindExtentions<SearchSupportExtension>();
                foreach (var ext in searchSupportExtensionExtentions)
                {
                    SearchSupportExtensionContainer.RegisterExtention(ext);
                    LoadedTypes.Add(ext);
                }

                // find all extentions of type FlowExtention
                var flowExtentions = walker.FindExtentions<FlowExtention>();
                foreach (var ext in flowExtentions)
                {
                    FlowExtentionContainer.RegisterExtention(ext);
                    LoadedTypes.Add(ext);
                }

                // find all extentions of type UIExtention
                var uiExtentions = walker.FindExtentions<UIExtention>();
                foreach (var ext in uiExtentions)
                {
                    UIExtentionContainer.RegisterExtention(ext);
                    LoadedTypes.Add(ext);
                }
            }
        }
        
        private void Container_ExtentionResolved(object sender, ExtentionEventArgs e)
        {
            StatelessExtentionResolved?.Invoke(this, e);
        }
    }
}
