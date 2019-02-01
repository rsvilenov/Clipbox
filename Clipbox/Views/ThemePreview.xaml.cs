using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Theme;
using Clipbox.Infrastructure.UI.Theme.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for ThemePreview.xaml
    /// </summary>
    public partial class ThemePreview : Window
    {

        private ObservableCollection<string> _brushNames;

        public ThemePreview()
        {
            InitializeComponent();
            
            ThemeManager.LoadThemeFromSystemTheme();

            _brushNames = new ObservableCollection<string>();

            foreach (ThemeBrush tb in (ThemeBrush[])Enum.GetValues(typeof(ThemeBrush)))
            {
                _brushNames.Add(tb.ToString());
            }

            MainListBox.ItemsSource = _brushNames;
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as StackPanel).SetBinding(StackPanel.BackgroundProperty, new Binding
            {
                Source = FindResource(_brushNames.FirstOrDefault(br => br == (string)(sender as StackPanel).Tag))
            });
        }
    }
}
