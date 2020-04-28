using Clipbox.Infrastructure.UI.Localization.Enums;
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xaml;

/// <summary>
/// This approach was inspired from this article:
/// https://codinginfinity.me/post/2015-05-10/localization_of_a_wpf_app_the_simple_approach
/// However, it has travelled a long way from there...
/// </summary>
namespace Clipbox.Infrastructure.UI.Localization
{
    /// <summary>
    /// A markup extension for localization
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class LocalizeExtension : MarkupExtension
    {
        [ConstructorArgument(nameof(Key))]
        public string Key { get; set; }

        [ConstructorArgument(nameof(ResourceLookupMode))]
        public ResourceLookupMode ResourceLookupMode { get; set; }

        public LocalizeExtension() { }

        public LocalizeExtension(string key)
        {
            Key = key;
        }

        public LocalizeExtension(string key, ResourceLookupMode resourceLookupMode)
        {
            Key = key;
            ResourceLookupMode = ResourceLookupMode;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Get the service which will be used to resolve the root container
            // of the element, which calls us via the markup extension.
            if (!(serviceProvider.GetService(typeof(IRootObjectProvider)) 
                is IRootObjectProvider rootObjectProvider))
            {
                return this;
            }

            TranslationSource src = null;

            // if we can resolve the root element, get an assembly-speciffic
            // translation source.
            if (ResourceLookupMode == ResourceLookupMode.CurrentAssembly &&
                rootObjectProvider.RootObject is FrameworkElement rootElement)
            {
                src = TranslationSourcePool.Instance.GetSource(rootElement.GetType().Assembly);
            }
            // otherwise, get the default translation source
            else
            {
                src = TranslationSourcePool.Instance.GetSource();
            }

            Binding binding = new Binding("[" + Key + "]")
            {
                Source = src,
                Mode = BindingMode.OneWay
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
