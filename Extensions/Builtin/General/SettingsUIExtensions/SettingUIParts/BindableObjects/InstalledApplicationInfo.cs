using Clipbox.Infrastructure.UI;
using System.Windows.Media.Imaging;

namespace Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts.BindableObjects
{
    public class InstalledApplicationInfo : BindableObject
    {
        public string InstallationLocation { get; set; }
        
        private string _productName;
        public string ProductName
        {
            get { return _productName; }
            set { SetField(ref _productName, value); }
        }
        
        private BitmapSource _icon;
        public BitmapSource Icon
        {
            get { return _icon; }
            set { SetField(ref _icon, value); }
        }
    }
}
