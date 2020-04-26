using Clipbox.Infrastructure.UI.Localization.Enums;
using System.Reflection;

namespace Clipbox.Infrastructure.UI.Localization
{
    interface IAssemblyAwareLocalizationProvider : ILocalizationProvider
    {
        string GetTranslation(string key, ResourceLookupMode mode, Assembly asm);
    }
}
