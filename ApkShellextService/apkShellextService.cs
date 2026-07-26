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

            // Determine System Drive (e.g., C:\ or D:\)
            string sysDrive = Path.GetPathRoot(Environment.SystemDirectory);
            string installTargetDir = Path.Combine(sysDrive, "ApkShellext_ByAliSakkaf");

            // Direct download link for release zip asset
            string downloadUrl = string.Format("https://github.com/alisakkaf/ApkShellext/releases/download/{0}/ApkShellext-{0}.zip", finalTag);
            string tempZipPath = Path.Combine(Path.GetTempPath(), string.Format("ApkShellext-{0}.zip", finalTag));
            string tempExtractDir = Path.Combine(Path.GetTempPath(), string.Format("ApkShellext_Extract_{0}", finalTag));

            using (WebClient wc = new WebClient()) {
                wc.Headers.Add("User-Agent", "ApkShellextService-Downloader");
                wc.DownloadFile(downloadUrl, tempZipPath);
            }

            if (!File.Exists(tempZipPath)) {
                LogEvent("Failed to download update package from " + downloadUrl, EventLogEntryType.Error);
                return false;
            }

            if (Directory.Exists(tempExtractDir)) {
                try { Directory.Delete(tempExtractDir, true); } catch { }
            }
            ZipFile.ExtractToDirectory(tempZipPath, tempExtractDir);

            // Ask user or execute update safely
            bool shouldUpdate = false;
            try {
                int result = MessageBox(IntPtr.Zero, 
                    "A new update (" + finalTag + ") for ApkShellext is available!\n\nWould you like to install it automatically now?", 
                    "ApkShellext Auto-Updater", 
                    MB_YESNO | MB_ICONQUESTION | MB_SERVICE_NOTIFICATION);
                if (result == IDYES) {
                    shouldUpdate = true;
                }
            } catch {
                shouldUpdate = true;
            }

            if (shouldUpdate) {
                ExecuteSilentInstall(tempExtractDir, installTargetDir);
                return true;
            }

            return false;
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

        private void ExecuteSilentInstall(string extractDir, string targetDir) {
            try {
                LogEvent("Starting update installation to " + targetDir, EventLogEntryType.Information);

                // 1. Detect current install directory if registered
                string currentInstalledDir = "";
                try {
                    using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"CLSID\{6F1D5E99-4A9B-4D4F-8C8B-8D75A0B1A7DF}\InprocServer32")) {
                        if (key != null) {
                            string codebase = key.GetValue("CodeBase") as string;
                            if (!string.IsNullOrEmpty(codebase)) {
                                Uri uri = new Uri(codebase);
                                currentInstalledDir = Path.GetDirectoryName(uri.LocalPath);
                            }
                        }
                    }
                } catch { }

                // 2. Run uninstall.bat elevated on old directory if present
                if (!string.IsNullOrEmpty(currentInstalledDir) && Directory.Exists(currentInstalledDir)) {
                    string oldUninstallBat = Path.Combine(currentInstalledDir, "uninstall.bat");
                    if (File.Exists(oldUninstallBat)) {
                        RunCommandElevated(oldUninstallBat);
                    }
                }

                // 3. Create target directory (%SystemDrive%\ApkShellext_ByAliSakkaf)
                if (!Directory.Exists(targetDir)) {
                    Directory.CreateDirectory(targetDir);
                }

                // 4. Copy files recursively with lock safety fallback
                foreach (string dirPath in Directory.GetDirectories(extractDir, "*", SearchOption.AllDirectories)) {
                    Directory.CreateDirectory(dirPath.Replace(extractDir, targetDir));
                }
                foreach (string filePath in Directory.GetFiles(extractDir, "*.*", SearchOption.AllDirectories)) {
                    string destPath = filePath.Replace(extractDir, targetDir);
                    try {
                        File.Copy(filePath, destPath, true);
                    } catch {
                        try {
                            if (File.Exists(destPath)) {
                                string oldFile = destPath + ".old_" + DateTime.Now.Ticks;
                                File.Move(destPath, oldFile);
                            }
                            File.Copy(filePath, destPath, true);
                        } catch { }
                    }
                }

                // 5. Run install.bat elevated in the new target directory
                string newInstallBat = Path.Combine(targetDir, "install.bat");
                if (File.Exists(newInstallBat)) {
                    RunCommandElevated(newInstallBat);
                }

                LogEvent("Successfully updated ApkShellext to target directory: " + targetDir, EventLogEntryType.Information);
            } catch (Exception ex) {
                LogEvent("Error executing silent update: " + ex.Message, EventLogEntryType.Error);
            } finally {
                EnsureExplorerRunning();
            }
        }

        private void RunCommandElevated(string batFilePath) {
            try {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = batFilePath;
                psi.WorkingDirectory = Path.GetDirectoryName(batFilePath);
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                using (Process proc = Process.Start(psi)) {
                    if (proc != null) {
                        proc.WaitForExit();
                    }
                }
            } catch (Exception ex) {
                LogEvent("Error running elevated bat script " + batFilePath + ": " + ex.Message, EventLogEntryType.Warning);
            }
        }

        private void EnsureExplorerRunning() {
            try {
                Process[] procs = Process.GetProcessesByName("explorer");
                if (procs == null || procs.Length == 0) {
                    ProcessStartInfo psi = new ProcessStartInfo("explorer.exe");
                    psi.UseShellExecute = true;
                    Process.Start(psi);
                }
            } catch { }
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
                Console.WriteLine("Webserver running...");
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
}
