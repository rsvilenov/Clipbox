using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using System;

namespace Clipbox.Core.Extensibility.Attributes
{
    // TODO: implement dependencies some day, maybe...
    [AttributeUsage(AttributeTargets.Class)]
    public class FlowDependencyAttribute : Attribute
    {
        public ParametrizedExtension DependencyExtension { get; }

        public FlowDependencyAttribute(ParametrizedExtension depencencyExtension)
        {
            DependencyExtension = depencencyExtension;
        }
    }
}
