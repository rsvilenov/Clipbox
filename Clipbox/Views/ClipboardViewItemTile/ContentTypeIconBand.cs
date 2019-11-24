using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Infrastructure.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipbox.Views.ClipboardViewItemTile
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:Clipbox.Views.ClipboardViewItemTile"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:Clipbox.Views.ClipboardViewItemTile;assembly=Clipbox.Views.ClipboardViewItemTile"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:ContentTypeIconBand/>
    ///
    /// </summary>
    [TemplatePart(Name = "PART_ItemsControl", Type = typeof(ItemsControl))]
    public class ContentTypeIconBand : Control
    {
        static ContentTypeIconBand()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContentTypeIconBand), new FrameworkPropertyMetadata(typeof(ContentTypeIconBand)));
        }
        
        public override void OnApplyTemplate()
        {
            //(GetTemplateChild("PART_ItemsControl") as ItemsControl).DataContext = this;
        }
        

        public bool IsGroupItem
        {
            get { return (bool)GetValue(IsGroupItemProperty); }
            set { SetValue(IsGroupItemProperty, value); }
        }

        public static readonly DependencyProperty IsGroupItemProperty =
            DependencyProperty.Register("IsGroupItem", typeof(bool), typeof(ContentTypeIconBand), new PropertyMetadata(false));

        public AsyncObservableCollection<ClipboardContentClass> ContentClasses
        {
            get { return (AsyncObservableCollection<ClipboardContentClass>)GetValue(ContentClassesProperty); }
            set { SetValue(ContentClassesProperty, value); }
        }

        public static readonly DependencyProperty ContentClassesProperty =
            DependencyProperty.Register("ContentClasses", typeof(AsyncObservableCollection<ClipboardContentClass>), typeof(ContentTypeIconBand), new PropertyMetadata(new AsyncObservableCollection<ClipboardContentClass>()));

    }
}
