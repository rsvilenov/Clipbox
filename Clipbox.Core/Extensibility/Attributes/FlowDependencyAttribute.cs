using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System;

namespace Clipbox.Core.Extensibility.Attributes
{
    // TODO: implement dependencies some day, maybe...
    [AttributeUsage(AttributeTargets.Class)]
    public class FlowDependencyAttribute : Attribute
    {
        public ParametrizedExtention DependencyExtention { get; }

        public FlowDependencyAttribute(ParametrizedExtention depencencyExtention)
        {
            DependencyExtention = depencencyExtention;
        }
    }
}
