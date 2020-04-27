using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
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
        List<Type> _allExtensionTypes;

        private AssemblyWalker()
        {
            _allExtensionTypes = new List<Type>();
        }

        public static AssemblyWalker FromAssembly(Assembly assembly)
        {
            var instance = new AssemblyWalker();
            instance.LoadAllExtensions(assembly);

            return instance;
        }


        private void LoadAllExtensions(Assembly assembly)
        {
            var extensionTypes = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IClipboxExtension)));
            foreach (var type in extensionTypes)
            {
                var extensionAttributes = (ClipboxExtensionAttribute[])type.GetCustomAttributes(typeof(ClipboxExtensionAttribute), true);
                if (!extensionAttributes.Any())
                {
                    throw new NotSupportedException($"Type {type.FullName} which implements IClipboxExtension should also have ClipboxExtension attribute.");
                }
                _allExtensionTypes.Add(type);
            }
        }

        /// <summary>
        /// Find types, which implement IClipboxExtension and have ClipboxExtension attribute
        /// </summary>
        /// <typeparam name="T">A derivative type of IClipboxExtension</typeparam>
        /// <returns>A list of all types</returns>
        public IEnumerable<Type> FindExtensions<T>()
            where T : IClipboxExtension
        {
            Type typeToSearch = typeof(T);
            return _allExtensionTypes.
                Where(t => t == typeToSearch // exact matches
                    || t.GetInterfaces().Contains(typeToSearch) // interface implementations
                    || t.IsSubclassOf(typeToSearch)); // virtual/abstract class subclasses
        }
    }
}
