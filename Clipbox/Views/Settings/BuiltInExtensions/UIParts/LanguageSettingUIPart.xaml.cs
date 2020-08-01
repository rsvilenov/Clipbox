using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Infrastructure.UI.Localization;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Media;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for LanguageSettingUIPart.xaml
    /// </summary>
    public partial class LanguageSettingUIPart : SettingUIPart<string>
    {
        public LanguageSettingUIPart()
        {
            InitializeComponent();
            PopulateAvailableCultures();
            TranslateCultureNames();

            this.mainGrid.DataContext = this;
        }

        private void PopulateAvailableCultures()
        {
            AvailableCultures = new AsyncObservableCollection<Culture>
            {
                new Culture
                {
                    Code = "en-US"
                },
                new Culture
                {
                    Code = "bg-BG"
                }
            };
        }

        private void TranslateCultureNames()
        {
            foreach (Culture culture in AvailableCultures)
            {
                culture.Name = TranslationSourcePool.Instance.GetSource(this.GetType().Assembly).GetTranslation(culture.Code);
            }
        }

        private AsyncObservableCollection<Culture> AvailableCultures
        {
            get { return (AsyncObservableCollection<Culture>)GetValue(AvailableCulturesProperty); }
            set { SetValue(AvailableCulturesProperty, value); }
        }

        private static readonly DependencyProperty AvailableCulturesProperty =
            DependencyProperty.Register(nameof(AvailableCultures), typeof(AsyncObservableCollection<Culture>), typeof(LanguageSettingUIPart), new PropertyMetadata(null));

        private string SelectedCulture
        {
            get { return (string)GetValue(SelectedCultureProperty); }
            set { SetValue(SelectedCultureProperty, value); }
        }

        private static readonly DependencyProperty SelectedCultureProperty =
            DependencyProperty.Register(nameof(SelectedCulture), typeof(string), typeof(LanguageSettingUIPart), new PropertyMetadata(string.Empty, SelectedCulture_PropertyChanged));

        private static void SelectedCulture_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (LanguageSettingUIPart)d;
            self.NotifySettingModified();
            self.TranslateCultureNames();
        }

        public override string GetSettingValue()
        {
            return SelectedCulture;
        }

        public override void SetSettingValue(string value)
        {
            SelectedCulture = value;
        }

        private class Culture : BindableObject
        {
            private string _code;
            public string Code
            {
                get { return _code; }
                set { SetField(ref _code, value); }
            }

            private string _name;
            public string Name
            {
                get { return _name; }
                set { SetField(ref _name, value); }
            }

            private ImageSource _flagImageSource;
            public ImageSource FlagImageSource
            {
                get { return _flagImageSource; }
                set { SetField(ref _flagImageSource, value); }
            }

        }
    }
}
