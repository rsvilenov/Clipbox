using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Clipbpx.SetUpDownloader
{
    public partial class DownloaderForm : Form
    {
        private readonly Logger _logger;
        private readonly string _checksumUrl;
        private readonly string _downloadUrl;

        public DownloaderForm()
        {
            _logger = new Logger();

            _checksumUrl = EnvironmentUtils.Is64BitOs() ?
                 ProductConstants.PRODUCT_DOWNLOAD_HASH_X64_URL : ProductConstants.PRODUCT_DOWNLOAD_HASH_X86_URL;

            _downloadUrl = EnvironmentUtils.Is64BitOs() ?
               ProductConstants.PRODUCT_DOWNLOAD_X64_URL : ProductConstants.PRODUCT_DOWNLOAD_X86_URL;


            try
            {
                _logger.Log("Initializing downloader form");
                Icon = Icon.FromHandle(Properties.Resources.Clip_WF.GetHicon());
                StartPosition = FormStartPosition.CenterScreen;

                InitializeComponent();

                Text = $"{ProductConstants.PRODUCT_NAME} setup";

                // add a "fake" border to the window,
                // which is the result of the difference in
                // color between the form and the panel,
                // as well as the 1px margin of the panel.
                panel1.Top = 1;
                panel1.Left = 1;
                panel1.Width = this.Width - 2;
                panel1.Height = this.Height - 2;

                // the window title
                lblProductName.Text = ProductConstants.PRODUCT_NAME;

                // reset the progressbar's value - some test value may be set in the designer
                downloadProgressBar.Value = 0;
            }
            catch (Exception ex)
            {
                FailWithMessageBox("Sorry. An error occurred.", ex.ToString());
            }

            // show it as fast as possible and then do the rest
            Shown += DownloaderForm_Shown;
        }

        private void DownloaderForm_Shown(object sender, EventArgs args)
        {
            // force all controls to be rendered first
            Application.DoEvents();

            // put it to the foreground
            Activate();

            // check for Windows 10
            if (!EnvironmentUtils.IsWindows10())
            {
                string errorMsg = "Windows 10 is required.";
                FailWithMessageBox(errorMsg, errorMsg);
                return;
            }

            // start downloading the actual setup file
            PerformDownload();
        }

        private void PerformDownload()
        {
            string tempFilePath = System.IO.Path.GetTempFileName();
            _logger.Log($"Temp path for the downloaded setup file: {tempFilePath}");

            
            _logger.Log($"Checksum url: {_checksumUrl}");
            _logger.Log($"Setup url: {_downloadUrl}");

            WebClient webClient = new WebClient();
            webClient.Headers.Add("User-Agent", "Clipbox Downloader 1.0");

            string md5Hash = string.Empty;
            webClient.DownloadProgressChanged += (s, e) =>
            {
                downloadProgressBar.Value = e.ProgressPercentage;
            };

            webClient.DownloadStringCompleted += (s, e) =>
            {
                if (e.Error != null)
                {
                    FailWithMessageBox("Download failed. Please, check your Internet connection and try again. If the error keeps occurring, you can still install Clipbox by downloading an offline installer from the website.", e.Error.ToString());
                }
                else
                {
                    md5Hash = e.Result;
                    _logger.Log($"Got expected checksum: {md5Hash}");
                    _logger.Log("Starting the actual download.");

                    webClient.DownloadFileAsync(new Uri(_downloadUrl),
                        tempFilePath);
                }
            };

            webClient.DownloadFileCompleted += (s, e) =>
            {
                if (e.Error != null)
                {
                    try { File.Delete(tempFilePath); } catch { }
                    FailWithMessageBox("Download failed. Please, check your Internet connection and try again. If the error keeps occurring, you can still install Clipbox by downloading an offline installer from the website.", e.Error.ToString());
                }
                else
                {
                    _logger.Log("Creating checksum checker thread.");
                    Thread checkThread = new Thread(new ThreadStart(() =>
                    {
                        bool checkSumIsOk = false;

                        try
                        {
                            using (var md5 = MD5.Create())
                            using (var stream = File.OpenRead(tempFilePath))
                            {
                                byte[] hashBytes = md5.ComputeHash(stream);

                                // Convert the byte array to hexadecimal string
                                StringBuilder sb = new StringBuilder();
                                for (int i = 0; i < hashBytes.Length; i++)
                                {
                                    sb.Append(hashBytes[i].ToString("X2"));
                                }

                                checkSumIsOk = sb.ToString() == md5Hash;
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.Log($"Error while checking checksum: {ex}");
                        }

                        if (checkSumIsOk)
                        {
                            _logger.Log("Checksum verification ok. Starting the downloaded setup executable.");
                            StartDownloadedSetup(tempFilePath);
                        }
                        else
                        {
                            try { File.Delete(tempFilePath); } catch { }
                            FailWithMessageBox("Download failed. You can still install Clipbox by downloading an offline installer from the website.", "Checksum check failed.");
                        }
                    }));

                    try
                    {
                        _logger.Log("Starting the thread verification thread.");
                        checkThread.Start();
                    }
                    catch(Exception ex)
                    {
                        try { File.Delete(tempFilePath); } catch { }
                        FailWithMessageBox("Download failed. You can still install Clipbox by downloading an offline installer from the website.", ex.ToString());
                    }
                }

            };

            try
            {
                _logger.Log($"Fetching the expected checksum from {_checksumUrl}.");

                webClient.DownloadStringAsync(new Uri(_checksumUrl));
            }
            catch (Exception ex)
            {
                try { File.Delete(tempFilePath); } catch { }
                FailWithMessageBox("Download failed. Are you connected to the Internet? You can still install Clipbox by downloading an offline installer from the website.", ex.ToString());
            }
        }

        private void FailWithMessageBox(string messageBoxMsg, string logMsg)
        {
            _logger.Log(logMsg);
            ShowError(messageBoxMsg);
            Application.Exit();
        }

        private void StartDownloadedSetup(string tempFilePath)
        {
            string setupPath = $"{tempFilePath}.exe";

            try
            {
                File.Move(tempFilePath, setupPath);

                using (Process p = Process.Start(setupPath))
                {
                    // hide the form
                    this.Invoke(new MethodInvoker(delegate ()
                    {
                        Hide();
                    }));
                    

                    // wait for the setup to finish, so that we can delete the file afterwards
                    _logger.Log("Waiting for the setup program to complete.");
                    p.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString());
                ShowError("Error running the setup program.");
            }

            try
            {
                // delete the file
                _logger.Log($"Deleting the setup executable ({setupPath}).");
                File.Delete(setupPath);
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString());
            }

            Application.Exit();

        }

        private static void ShowError(string errorMessage)
        {
            MessageBox.Show(
                errorMessage,
                $"{ProductConstants.PRODUCT_NAME} setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }


        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);

            // make the etire form movable
            if (message.Msg == WM_NCHITTEST
                && (int)message.Result == HTCLIENT)
            {
                message.Result = (IntPtr)HTCAPTION;
            }
        }
    }
}
