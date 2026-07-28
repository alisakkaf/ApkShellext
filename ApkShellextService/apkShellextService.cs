using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.ServiceProcess;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ApkShellext {
    class apkShellextService : ServiceBase {
        private System.Threading.Timer updateCheckTimer;

        public apkShellextService() {
            ServiceName = "ApkShellext Service";
            EventLog.Log = "Application";
            
            CanHandlePowerEvent = false;
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = false;
            CanShutdown = false;
            CanStop = true;
        }

        static void Main(string[] args) {
            if (args != null && args.Length > 0) {
                string firstArg = args[0];
                if (firstArg.Equals("/install", StringComparison.OrdinalIgnoreCase)) {
                    string packagePath = args.Length > 1 ? args[1] : "";
                    if (!string.IsNullOrEmpty(packagePath) && File.Exists(packagePath)) {
                        ShowAdbInstallDialog(packagePath);
                        return;
                    }
                }
                if (firstArg.Equals("/preferences", StringComparison.OrdinalIgnoreCase) ||
                    firstArg.Equals("/settings", StringComparison.OrdinalIgnoreCase)) {
                    ShowPreferencesDialog();
                    return;
                }
                if (File.Exists(firstArg) && (firstArg.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) ||
                                             firstArg.EndsWith(".xapk", StringComparison.OrdinalIgnoreCase) ||
                                             firstArg.EndsWith(".apks", StringComparison.OrdinalIgnoreCase) ||
                                             firstArg.EndsWith(".apkm", StringComparison.OrdinalIgnoreCase) ||
                                             firstArg.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase) ||
                                             firstArg.EndsWith(".appx", StringComparison.OrdinalIgnoreCase) ||
                                             firstArg.EndsWith(".appxbundle", StringComparison.OrdinalIgnoreCase))) {
                    ShowAdbInstallDialog(firstArg);
                    return;
                }
            }

            bool interactive = Array.Exists(args, arg => arg.Equals("/interactive", StringComparison.OrdinalIgnoreCase));
            if (interactive || Environment.UserInteractive) {
                try {
                    apkShellextService service = new apkShellextService();
                    service.OnStartPublic(args);
                    System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                } catch { }
            } else {
                ServiceBase.Run(new apkShellextService());
            }
        }

        private static void ShowAdbInstallDialog(string packagePath) {
            try {
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApkShellext.dll");
                if (File.Exists(dllPath)) {
                    Assembly asm = Assembly.LoadFrom(dllPath);
                    Type formType = asm.GetType("ApkShellext.AdbInstallForm");
                    if (formType != null) {
                        object formInst = Activator.CreateInstance(formType, new object[] { packagePath });
                        MethodInfo showDialogMethod = formType.GetMethod("ShowDialog", new Type[0]);
                        if (showDialogMethod != null) {
                            showDialogMethod.Invoke(formInst, null);
                            return;
                        }
                    }
                }
            } catch (Exception ex) {
                System.Windows.Forms.MessageBox.Show("Failed to launch ADB Installer: " + ex.Message, "ApkShellext", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private static void ShowPreferencesDialog() {
            try {
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApkShellext.dll");
                if (File.Exists(dllPath)) {
                    Assembly asm = Assembly.LoadFrom(dllPath);
                    Type formType = asm.GetType("ApkShellext.Preferences");
                    if (formType != null) {
                        object formInst = Activator.CreateInstance(formType);
                        MethodInfo showDialogMethod = formType.GetMethod("ShowDialog", new Type[0]);
                        if (showDialogMethod != null) {
                            showDialogMethod.Invoke(formInst, null);
                            return;
                        }
                    }
                }
            } catch (Exception ex) {
                System.Windows.Forms.MessageBox.Show("Failed to launch Preferences: " + ex.Message, "ApkShellext", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        public void OnStartPublic(string[] args) {
            OnStart(args);
        }

        protected override void Dispose(bool disposing) {
            if (disposing && updateCheckTimer != null) {
                updateCheckTimer.Dispose();
                updateCheckTimer = null;
            }
            base.Dispose(disposing);
        }

        private WebServer ws;

        private static string LocalIPAddress() {
            try {
                var card = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0];
                var str = card.GetIPProperties().GatewayAddresses;
                
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList) {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                        return ip.ToString();
                    }
                }
            } catch {}
            return "127.0.0.1";
        }

        protected override void OnStart(string[] args) {
            base.OnStart(args);

            string[] prefixes = new string[] {
                @"http://*:42728/",
                @"http://localhost:42728/",
                @"http://"+LocalIPAddress()+@":42728/",
                @"http://127.0.0.1:42728/"
            };

            try {
                ws = new WebServer(SendResponse, prefixes);
                ws.Run();
            } catch (Exception ex) {
                LogEvent("Failed to start WebServer: " + ex.Message, EventLogEntryType.Warning);
            }

            // Start background Auto-Updater check after 10 seconds, then check every 12 hours
            updateCheckTimer = new System.Threading.Timer(CheckForUpdatesCallback, null, 10000, 12 * 3600 * 1000);
        }

        protected override void OnStop() {
            base.OnStop();
            if (ws != null) {
                try { ws.Stop(); } catch {}
            }
            if (updateCheckTimer != null) {
                updateCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private int retryCount = 0;
        private bool updateFinished = false;

        private void CheckForUpdatesCallback(object state) {
            if (updateFinished) return;

            bool result = false;
            try {
                result = PerformAutoUpdateCheck();
            } catch (Exception ex) {
                LogEvent("Error during update check: " + ex.Message, EventLogEntryType.Error);
                result = false;
            }

            if (result) {
                updateFinished = true;
                if (updateCheckTimer != null) {
                    updateCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                return;
            }

            retryCount++;
            if (retryCount == 1) {
                LogEvent("Update check did not find an update or network was unavailable. Retrying in 5 minutes (Attempt 1)...", EventLogEntryType.Information);
                if (updateCheckTimer != null) {
                    updateCheckTimer.Change(5 * 60 * 1000, Timeout.Infinite);
                }
            } else if (retryCount <= 4) {
                LogEvent("Update check did not find an update. Retrying in 15 minutes (Attempt " + retryCount + " of 4)...", EventLogEntryType.Information);
                if (updateCheckTimer != null) {
                    updateCheckTimer.Change(15 * 60 * 1000, Timeout.Infinite);
                }
            } else {
                updateFinished = true;
                LogEvent("Completed maximum update check retries. Stopping update checks until service restarts.", EventLogEntryType.Information);
                if (updateCheckTimer != null) {
                    updateCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        private const uint MB_YESNO = 0x00000004;
        private const uint MB_ICONQUESTION = 0x00000020;
        private const uint MB_SERVICE_NOTIFICATION = 0x00200000;
        private const int IDYES = 6;

        private bool PerformAutoUpdateCheck() {
            try {
                string latestUrl = "https://github.com/alisakkaf/ApkShellext/releases/latest";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(latestUrl);
                request.Method = "HEAD";
                request.AllowAutoRedirect = true;
                request.UserAgent = "ApkShellextService-Updater";
                request.Timeout = 15000;

                string finalTag = "";
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    string finalUrl = response.ResponseUri.ToString();
                    int lastSlash = finalUrl.LastIndexOf('/');
                    if (lastSlash >= 0) {
                        finalTag = finalUrl.Substring(lastSlash + 1);
                    }
                }

                if (string.IsNullOrEmpty(finalTag)) return false;

                string cleanVersionStr = finalTag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? finalTag.Substring(1) : finalTag;
                Version latestVer;
                if (!Version.TryParse(cleanVersionStr, out latestVer)) return false;

                Version currentVer = Assembly.GetExecutingAssembly().GetName().Version;
                if (!IsNewerVersion(latestVer, currentVer)) {
                    LogEvent("ApkShellext is up to date (Current: " + currentVer.ToString() + ", GitHub: " + finalTag + ").", EventLogEntryType.Information);
                    return false;
                }

                LogEvent("New update detected: " + finalTag + " (Current: " + currentVer.ToString() + ")", EventLogEntryType.Information);

                // Format clean 3-part version tag (e.g. v1.2.1)
                string versionTag = string.Format("v{0}.{1}.{2}", latestVer.Major >= 0 ? latestVer.Major : 1, latestVer.Minor >= 0 ? latestVer.Minor : 0, latestVer.Build >= 0 ? latestVer.Build : 0);

                // Determine System Drive target folder (e.g., C:\ApkShellext_v1.2.1)
                string sysDrive = Path.GetPathRoot(Environment.SystemDirectory);
                string targetVersionDir = Path.Combine(sysDrive, string.Format("ApkShellext_{0}", versionTag));

                // Direct download link for release zip asset
                string downloadUrl = string.Format("https://github.com/alisakkaf/ApkShellext/releases/download/{0}/ApkShellext-{0}.zip", versionTag);
                string tempZipPath = Path.Combine(Path.GetTempPath(), string.Format("ApkShellext-{0}.zip", versionTag));

                using (WebClient wc = new WebClient()) {
                    wc.Headers.Add("User-Agent", "ApkShellextService-Downloader");
                    wc.DownloadFile(downloadUrl, tempZipPath);
                }

                if (!File.Exists(tempZipPath)) {
                    LogEvent("Failed to download update package from " + downloadUrl, EventLogEntryType.Error);
                    return false;
                }

                // Extract package into new target version folder (e.g., C:\ApkShellext_v1.2.1)
                if (Directory.Exists(targetVersionDir)) {
                    try { Directory.Delete(targetVersionDir, true); } catch { }
                }
                Directory.CreateDirectory(targetVersionDir);
                ZipFile.ExtractToDirectory(tempZipPath, targetVersionDir);

                // Verify extraction correctness (must contain install.bat and ApkShellext.dll)
                string installBat = Path.Combine(targetVersionDir, "install.bat");
                string dllPath = Path.Combine(targetVersionDir, "ApkShellext.dll");
                if (!File.Exists(installBat) || !File.Exists(dllPath)) {
                    LogEvent("Update extraction verification failed in " + targetVersionDir, EventLogEntryType.Error);
                    return false;
                }

                // Clean up temp ZIP file
                try { File.Delete(tempZipPath); } catch { }

                LogEvent("New update extracted successfully to: " + targetVersionDir, EventLogEntryType.Information);

                string msgText = string.Format(
                    "A new update ({0}) for ApkShellext has been downloaded and extracted to:\n{1}\n\n" +
                    "To complete the update:\n" +
                    "1. If a previous version is installed, run uninstall.bat in its folder.\n" +
                    "2. Right-click install.bat in the new folder and select 'Run as Administrator'.\n\n" +
                    "Would you like to open the update folder now?",
                    versionTag, targetVersionDir);

                // Prompt user using native WinForms dialog with app icon and taskbar icon
                bool shouldOpenFolder = false;
                try {
                    using (AutoUpdateForm form = new AutoUpdateForm(versionTag, targetVersionDir)) {
                        if (form.ShowDialog() == System.Windows.Forms.DialogResult.Yes) {
                            shouldOpenFolder = true;
                        }
                    }
                } catch {
                    try {
                        int result = MessageBox(IntPtr.Zero, msgText, "ApkShellext Auto-Updater (" + versionTag + ")", MB_YESNO | MB_ICONQUESTION | MB_SERVICE_NOTIFICATION);
                        if (result == IDYES) {
                            shouldOpenFolder = true;
                        }
                    } catch {
                        shouldOpenFolder = true;
                    }
                }

                if (shouldOpenFolder) {
                    try {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = targetVersionDir;
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    } catch {
                        try {
                            Process.Start("explorer.exe", "\"" + targetVersionDir + "\"");
                        } catch { }
                    }
                }

                return true;
            } catch (Exception ex) {
                LogEvent("Error performing update check or extraction: " + ex.Message, EventLogEntryType.Error);
                return false;
            }
        }

        private bool IsNewerVersion(Version latest, Version current) {
            if (latest == null || current == null) return false;

            int latestMajor = latest.Major >= 0 ? latest.Major : 0;
            int latestMinor = latest.Minor >= 0 ? latest.Minor : 0;
            int latestBuild = latest.Build >= 0 ? latest.Build : 0;

            int currentMajor = current.Major >= 0 ? current.Major : 0;
            int currentMinor = current.Minor >= 0 ? current.Minor : 0;
            int currentBuild = current.Build >= 0 ? current.Build : 0;

            if (latestMajor > currentMajor) return true;
            if (latestMajor < currentMajor) return false;

            if (latestMinor > currentMinor) return true;
            if (latestMinor < currentMinor) return false;

            if (latestBuild > currentBuild) return true;

            return false;
        }

        private void LogEvent(string msg, EventLogEntryType type) {
            try {
                EventLog.WriteEntry("ApkShellext Service", msg, type);
            } catch {}
        }

        Dictionary<string, string> pathList = new Dictionary<string,string>();
        
        public string SendResponse(HttpListenerRequest request) {
            if (request.QueryString["md5"] != null) {
                if (!pathList.ContainsKey(request.QueryString["md5"])) {
                    pathList.Add(request.QueryString["md5"], request.QueryString["path"]);
                }
                return "";
            } else {
                string md5 = request.RawUrl.Replace(@"/", "");
                if (pathList.ContainsKey(md5))
                    return pathList[md5];
                return "";
            }
        }
    }


    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;
 
        public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");
 
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");
 
            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");
 
            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);
 
            _responderMethod = method;
            _listener.Start();
        }
 
        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method) { }
 
        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                string rstr = _responderMethod(ctx.Request);
                                if (rstr != "") {
                                    string filename = Path.GetFileName(rstr);
                                    using (FileStream fs = new FileStream(rstr, FileMode.Open)) {
                                        using (BinaryReader sr = new BinaryReader(fs)) {
                                            byte[] buf = sr.ReadBytes((int)sr.BaseStream.Length);
                                            ctx.Response.ContentType = "application/octet-stream";
                                            ctx.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + filename + "\"");
                                            ctx.Response.ContentLength64 = buf.Length;
                                            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                                        }
                                    }
                                }                         
                            }
                            catch (Exception ex){
                                EventLog log = new EventLog();
                                log.WriteEntry(ex.Message);
                            } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }
 
        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }

    public class AutoUpdateForm : System.Windows.Forms.Form {
        public AutoUpdateForm(string versionTag, string targetDir) {
            this.Text = "ApkShellext Auto-Updater (" + versionTag + ")";
            this.Size = new System.Drawing.Size(560, 310);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = true;
            this.BackColor = System.Drawing.Color.White;

            try {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            } catch { }

            System.Windows.Forms.PictureBox pbLogo = new System.Windows.Forms.PictureBox {
                Location = new System.Drawing.Point(24, 24),
                Size = new System.Drawing.Size(64, 64),
                SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
            };

            try {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                pbLogo.Image = System.Drawing.Icon.ExtractAssociatedIcon(exePath).ToBitmap();
            } catch { }

            System.Windows.Forms.Label lblMsg = new System.Windows.Forms.Label {
                Location = new System.Drawing.Point(106, 20),
                Size = new System.Drawing.Size(420, 185),
                Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular),
                ForeColor = System.Drawing.Color.FromArgb(30, 30, 30),
                Text = string.Format(
                    "A new update ({0}) for ApkShellext has been downloaded and extracted to:\n{1}\n\n" +
                    "To complete the update:\n" +
                    "1. If a previous version is installed, run uninstall.bat in its folder.\n" +
                    "2. Right-click install.bat in the new folder and select 'Run as Administrator'.\n\n" +
                    "Would you like to open the update folder now?",
                    versionTag, targetDir)
            };

            System.Windows.Forms.Button btnYes = new System.Windows.Forms.Button {
                Text = "Yes",
                DialogResult = System.Windows.Forms.DialogResult.Yes,
                Location = new System.Drawing.Point(315, 220),
                Size = new System.Drawing.Size(100, 34),
                Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White
            };
            btnYes.FlatAppearance.BorderSize = 0;

            System.Windows.Forms.Button btnNo = new System.Windows.Forms.Button {
                Text = "No",
                DialogResult = System.Windows.Forms.DialogResult.No,
                Location = new System.Drawing.Point(425, 220),
                Size = new System.Drawing.Size(100, 34),
                Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(235, 238, 242),
                ForeColor = System.Drawing.Color.FromArgb(50, 50, 50)
            };
            btnNo.FlatAppearance.BorderSize = 0;

            this.Controls.Add(pbLogo);
            this.Controls.Add(lblMsg);
            this.Controls.Add(btnYes);
            this.Controls.Add(btnNo);
            this.AcceptButton = btnYes;
            this.CancelButton = btnNo;
        }
    }
}
