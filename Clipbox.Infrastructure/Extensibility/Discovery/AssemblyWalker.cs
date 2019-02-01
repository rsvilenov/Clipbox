using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Clipbox.Infrastructure.Extensibility.Discovery
{
    /// <summary>
    /// Finds implementations of IClipboxExtention in an assembly
    /// </summary>
    public class AssemblyWalker
    {
        List<Type> _allExtentionTypes;

        private AssemblyWalker()
        {
            _allExtentionTypes = new List<Type>();
        }

        public static AssemblyWalker FromAssembly(Assembly assembly)
        {
            var instance = new AssemblyWalker();
            instance.LoadAllExtentions(assembly);

            return instance;
        }


        private void LoadAllExtentions(Assembly assembly)
        {
            var extentionTypes = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IClipboxExtention)));
            foreach (var type in extentionTypes)
            {
                var extentionAttributes = (ClipboxExtentionAttribute[])type.GetCustomAttributes(typeof(ClipboxExtentionAttribute), true);
                if (!extentionAttributes.Any())
                {
                    throw new NotSupportedException($"Type {type.FullName} which implements IClipboxExtention should also have ClipboxExtention attribute.");
                }
                _allExtentionTypes.Add(type);
            }
        }

        /// <summary>
        /// Find types, which implement IClipboxExtention and have ClipboxExtention attribute
        /// </summary>
        /// <typeparam name="T">A derivative type of IClipboxExtention</typeparam>
        /// <returns>A list of all types</returns>
        public IEnumerable<Type> FindExtentions<T>()
            where T : IClipboxExtention
        {
            Type typeToSearch = typeof(T);
            return _allExtentionTypes.
                Where(t => t == typeToSearch // exact matches
                    || t.GetInterfaces().Contains(typeToSearch) // interface implementations
                    || t.IsSubclassOf(typeToSearch)); // virtual/abstract class subclasses
        }
    }
}
