using System;
using System.Collections.Generic;

namespace Clipbox.Views.Settings.Interfaces
{
    public interface ICanResolveDependentExtensions
    {
        IEnumerable<Type> GetDependentExtensions(string extensionTypeFullName);
    }
}
