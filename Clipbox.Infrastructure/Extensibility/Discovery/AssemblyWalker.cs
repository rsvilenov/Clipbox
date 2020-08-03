using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Infrastructure.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Clipbox.Infrastructure.Extensibility.Discovery
{
    /// <summary>
    /// Finds implementations of IClipboxExtension in an assembly
    /// </summary>
    public class AssemblyWalker
    {
        private readonly List<IExtensionInfo> _allExtensionTypes;

        private AssemblyWalker()
        {
            _allExtensionTypes = new List<IExtensionInfo>();
        }

        public static AssemblyWalker FromAssembly(Assembly assembly)
        {
            var instance = new AssemblyWalker();
            instance.LoadAllExtensions(assembly);

            return instance;
        }

        private void LoadAllExtensions(Assembly assembly)
        {
            var extensionTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces()
                    // matching by type in Reflection-Only Context is not possible
                    .Select(i => i.AssemblyQualifiedName)
                    .Contains(typeof(IClipboxExtension).AssemblyQualifiedName));

            foreach (var type in extensionTypes)
            {
                if (!ClipboxExtensionAttribute.IsDefinedFor(type))
                {
                    throw new NotSupportedException($"Type {type.FullName} which implements IClipboxExtension should also have ClipboxExtension attribute.");
                }

                IExtensionInfo extInfo = ClipboxExtensionAttribute.GetExtensionInfo(type);
                
                _allExtensionTypes.Add(extInfo);
            }
        }

        /// <summary>
        /// Find extensions, which implement IClipboxExtension and have ClipboxExtension attribute
        /// </summary>
        /// <typeparam name="T">A derivative type of IClipboxExtension</typeparam>
        /// <returns>A list of <see cref="IExtensionInfo" /></returns>
        public IEnumerable<IExtensionInfo> FindExtensions<T>()
            where T : IClipboxExtension
        {
            Type typeToSearch = typeof(T);
            return _allExtensionTypes.
                Where(extInfo => extInfo.ExtensionObjectType.AssemblyQualifiedName == typeToSearch.AssemblyQualifiedName // exact matches
                    || extInfo.ExtensionObjectType.GetBaseClassesAndInterfaces().Any(ts => ts.AssemblyQualifiedName == typeToSearch.AssemblyQualifiedName)); // interfaces or virtual/abstract base classes
        }
    }
}
