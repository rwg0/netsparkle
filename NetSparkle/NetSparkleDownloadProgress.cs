using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace AppLimit.NetSparkle
{
    public partial class NetSparkleDownloadProgress : Form
    {
        private String _tempName;
        private NetSparkleAppCastItem _item;
        private String _referencedAssembly;
        private Sparkle _sparkle;
        private Boolean _unattend;
        private WebClient _client;

        public NetSparkleDownloadProgress(Sparkle sparkle, NetSparkleAppCastItem item, String referencedAssembly, Image appIcon, Icon windowIcon, Boolean Unattend)
        {
            InitializeComponent();

            if (appIcon != null)
                imgAppIcon.Image = appIcon;

            if (windowIcon != null)
                Icon = windowIcon;

            // store the item
            _sparkle = sparkle;
            _item = item;
            _referencedAssembly = referencedAssembly;
            _unattend = Unattend;

            // init ui
            btnInstallAndReLaunch.Visible = false;
            lblHeader.Text = lblHeader.Text.Replace("APP", item.AppName + " " + item.Version);
            progressDownload.Maximum = 100;
            progressDownload.Minimum = 0;
            progressDownload.Step = 1;

            // show the right 
            Size = new Size(Size.Width, 127);
            lblSecurityHint.Visible = false;                
            
            // get the filename of the download lin
            String[] segments = item.DownloadLink.Split('/');
            String fileName = segments[segments.Length - 1];

            // get temp path
            _tempName = Path.GetTempFileName();

            // start async download
            _client = new WebClient();
            _client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Client_DownloadProgressChanged);
            _client.DownloadFileCompleted += new AsyncCompletedEventHandler(Client_DownloadFileCompleted);
            
            Uri url = new Uri(item.DownloadLink);
            url = sparkle.TransformSparkleUrl(url);

            _client.DownloadFileAsync(url, _tempName);
        }

        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<AsyncCompletedEventArgs>(Client_DownloadFileCompleted), new object[] {sender, e});
            }
            else
            {
                if (e.Error != null)
                {
                    MessageBox.Show(this, string.Format("The download of {2} {3} from \r\n\r\n{0}\r\n\r\nhas failed.\r\n\r\nError Detail : {1}", _item.DownloadLink, e.Error.Message, _item.AppName, _item.Version), "NetSparkle", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                var contentDisp = _client.ResponseHeaders["content-disposition"];
                if (!string.IsNullOrEmpty(contentDisp))
                    RenameDownload(contentDisp);

                progressDownload.Visible = false;
                btnInstallAndReLaunch.Visible = true;

                // report message            
                _sparkle.ReportDiagnosticMessage("Finished downloading file to: " + _tempName);

                if (!NetSparkleCheckAndInstall.CheckDSA(_sparkle, _item, _tempName))
                {
                    btnInstallAndReLaunch.Enabled = false;
                    Size = new Size(Size.Width, 159);
                    lblSecurityHint.Visible = true;
                    BackColor = Color.Tomato;
                }

                // Check the unattended mode
                if (_unattend)
                    btnInstallAndReLaunch_Click(null, null);
            }
        }

        private void RenameDownload(string contentDisp)
        {
            int index = contentDisp.IndexOf("filename=");
            if (index == -1)
                return;

            string suspectedName = contentDisp.Substring(index + "filename=".Length);

            if (string.IsNullOrEmpty(suspectedName))
                return;

            if (suspectedName[0] == '"')
            {
                RenameDownloadTo(suspectedName.Substring(1).Split('"').First());
                return;
            }

            RenameDownloadTo(suspectedName.Split(';').First());
        }

        private void RenameDownloadTo(string newName)
        {
            string guid = Guid.NewGuid().ToString();
            string tempfolder = Path.Combine(Path.GetTempPath(), guid);
            Directory.CreateDirectory(tempfolder);
            string newPath = Path.Combine(tempfolder, newName);
            try
            {
                if (File.Exists(newName))
                    File.Delete(newName);
                File.Move(_tempName, newPath);
                _tempName = newPath;
            }
            catch (Exception)
            {
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressDownload.Value = e.ProgressPercentage;            
        }

        private void btnInstallAndReLaunch_Click(object sender, EventArgs e)
        {
            NetSparkleCheckAndInstall.Install(_sparkle, _tempName, _sparkle.RestartApplication, _sparkle.InstallCommandOptions, _sparkle.ShutdownCallback);
        }
    }
}
