using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Clipbpx.SetUpDownloader.CustomControls
{
    public class HitTestTransparentLabel : Label
    {
        private readonly bool _isDesignMode;
        public HitTestTransparentLabel()
        {
            _isDesignMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
        }
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = (-1);

            if (!_isDesignMode && m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
            }
            else
            {
                base.WndProc(ref m);
            }
        }
    }
}
