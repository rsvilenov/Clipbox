using System.Globalization;

namespace Clipbox.Core.Localization
{
    public interface ITranslationSource
    {
        CultureInfo CurrentCulture { get; set; }
    }
}
