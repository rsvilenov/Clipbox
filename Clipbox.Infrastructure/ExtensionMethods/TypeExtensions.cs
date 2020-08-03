using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.ExtensionMethods
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets all base classes and interfaces of a type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetBaseClassesAndInterfaces(this Type type)
        {
            return type.BaseType == null || type.BaseType.AssemblyQualifiedName == typeof(object).AssemblyQualifiedName
                ? type.GetInterfaces()
                : Enumerable
                    .Repeat(type.BaseType, 1)
                    .Concat(type.GetInterfaces())
                    .Concat(GetBaseClassesAndInterfaces(type.BaseType))
                    .Distinct();
        }
    }
}
