using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace Clipbpx.SetUpDownloader
{
    public partial class DownloaderForm : Form
    {
        public DownloaderForm()
        {
            Icon = Icon.FromHandle(Properties.Resources.Clip_WF.GetHicon());
            StartPosition = FormStartPosition.CenterScreen;
            InitializeComponent();

            panel1.Top = 1;
            panel1.Left = 1;
            panel1.Width = this.Width - 2;
            panel1.Height = this.Height - 2;

            label1.Text = ProductConstants.PRODUCT_NAME;
            downloadProgressBar.Value = 0;

            // show it as fast as possible
            Shown += DownloaderForm_Shown;
        }

        private void DownloaderForm_Shown(object sender, EventArgs args)
        {
            // force all controls to be rendered first
            Application.DoEvents();

            // put it to the foreground
            Activate();

            string tempFilePath = System.IO.Path.GetTempFileName();

            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += (s, e) =>
            {
                downloadProgressBar.Value = e.ProgressPercentage;
            };
            webClient.DownloadFileCompleted += (s, e) =>
            {
                if (e.Error != null)
                {
                    try { System.IO.File.Delete(tempFilePath); } catch { }
                    MessageBox.Show(
                        "Download failed. Are you connected to the Internet?\n You can download an offline installer from the website.",
                        $"{ProductConstants.PRODUCT_NAME} setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                else
                {
                    System.IO.File.Move(tempFilePath, $"{tempFilePath}.exe");

                    Process.Start($"{tempFilePath}.exe");
                }

                Application.Exit();
            };

            try
            {
                webClient.DownloadFileAsync(new Uri(ProductConstants.PRODUCT_DOWNLOAD_URL),
                    tempFilePath);
            }
            catch (Exception ex)
            {
                try { System.IO.File.Delete(tempFilePath); } catch { }
                MessageBox.Show(
                        "Download failed. Are you connected to the Internet?\n You can download an offline installer from the website.",
                        $"{ProductConstants.PRODUCT_NAME} setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
            }
        }

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);

            if (message.Msg == WM_NCHITTEST 
                && (int)message.Result == HTCLIENT)
            {
                message.Result = (IntPtr)HTCAPTION;
            }
        }
    }
}
