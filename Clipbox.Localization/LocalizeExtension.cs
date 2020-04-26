using System.Windows.Data;

/// <summary>
/// This approach to localization comes from here:
/// https://codinginfinity.me/post/2015-05-10/localization_of_a_wpf_app_the_simple_approach
/// </summary>
namespace Clipbox.Infrastructure.UI.Localization
{
    public class LocalizeExtension : Binding
    {
        public LocalizeExtension(string name) : base("[" + name + "]")
        {
            this.Mode = BindingMode.OneWay;
            this.Source = TranslationSource.Instance;
        }
        
        //public override object ProvideValue(IServiceProvider serviceProvider)
        //{
        //    var rootObjectProvider = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
        //    var root = rootObjectProvider.RootObject;
        //    // do whatever you need to do here
        //}
    }
}
