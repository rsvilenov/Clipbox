using Clipbox.Infrastructure.UI;
using System.Windows.Media.Imaging;

namespace Clipbox.Views.FilterBand
{
    public class SourceApplicationInfo : BindableObject
    {
        public string ExecutablePath { get; set; }

        private string _executableName;
        public string ExecutableName
        {
            get { return _executableName; }
            set { SetField(ref _executableName, value); }
        }

        private string _fileDescription;
        public string FileDescription
        {
            get { return _fileDescription; }
            set { SetField(ref _fileDescription, value); }
        }

        private string _productName;
        public string ProductName
        {
            get { return _productName; }
            set { SetField(ref _productName, value); }
        }

        private string _formattedProductName;
        public string FormattedProductName
        {
            get { return _formattedProductName; }
            set { SetField(ref _formattedProductName, value); }
        }

        private BitmapSource _icon;
        public BitmapSource Icon
        {
            get { return _icon; }
            set { SetField(ref _icon, value); }
        }
    }
}
