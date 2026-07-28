using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ApkQuickReader;
using ApkShellext.Properties;

namespace ApkShellext {
    public class AdbInstallForm : Form {
        private string filePath;
        private PictureBox pbIcon;
        private Label lblAppName;
        private Label lblAppDetails;
        private ProgressBar progressBar;
        private Label lblStatus;
        private TextBox txtLog;
        private Button btnAction;
        private Process currentProcess;
        private bool isInstalling = false;
        private CancellationTokenSource cancellationTokenSource;

        private class AdbResult {
            public bool Success { get; set; }
            public string Output { get; set; }
        }

        public AdbInstallForm(string packagePath) {
            filePath = packagePath;
            InitializeComponent();
            LoadPackageInfo();
        }

        private void InitializeComponent() {
            this.Text = Utility.GetResourceString("menuInstallAdb", "Install on Device (ADB)");
            this.Size = new Size(640, 500);
            this.MinimumSize = new Size(580, 440);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.BackColor = Color.White;
            try {
                this.Icon = Icon.FromHandle(Utility.ResizeBitmap(Properties.NonLocalizeResources.logo, 32).GetHicon());
            } catch {
                this.Icon = SystemIcons.Application;
            }
            this.RightToLeft = Utility.IsRtl() ? RightToLeft.Yes : RightToLeft.No;

            pbIcon = new PictureBox {
                Location = new Point(20, 20),
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            lblAppName = new Label {
                Location = new Point(96, 20),
                Size = new Size(510, 26),
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = Path.GetFileName(filePath)
            };

            lblAppDetails = new Label {
                Location = new Point(96, 48),
                Size = new Size(510, 36),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.DimGray,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = filePath
            };

            progressBar = new ProgressBar {
                Location = new Point(20, 100),
                Size = new Size(584, 20),
                Style = ProgressBarStyle.Marquee,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblStatus = new Label {
                Location = new Point(20, 130),
                Width = 584,
                AutoSize = true,
                MaximumSize = new Size(584, 0),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = Utility.GetResourceString("strAdbInstalling", "Installing package on device...")
            };

            txtLog = new TextBox {
                Location = new Point(20, 182),
                Size = new Size(584, 215),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 9f),
                BackColor = Color.FromArgb(250, 251, 252),
                ForeColor = Color.FromArgb(40, 40, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            btnAction = new Button {
                Location = new Point(504, 410),
                Size = new Size(100, 34),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = Utility.GetResourceString("btnCancel", "Cancel"),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 238, 242),
                ForeColor = Color.FromArgb(50, 50, 50),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Click += BtnAction_Click;

            this.Controls.Add(pbIcon);
            this.Controls.Add(lblAppName);
            this.Controls.Add(lblAppDetails);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblStatus);
            this.Controls.Add(txtLog);
            this.Controls.Add(btnAction);

            this.FormClosing += AdbInstallForm_FormClosing;
            this.Shown += AdbInstallForm_Shown;
            this.Resize += (s, e) => UpdateLayoutPositions();
        }

        private void LoadPackageInfo() {
            try {
                using (AppPackageReader reader = AppPackageReader.Read(filePath)) {
                    if (reader != null) {
                        lblAppName.Text = reader.AppName;
                        lblAppDetails.Text = string.Format("{0} ({1}) - {2:0.0} MB",
                            reader.PackageName,
                            reader.Version,
                            new FileInfo(filePath).Length / (1024.0 * 1024.0));
                        if (reader.Icon != null) {
                            pbIcon.Image = (Bitmap)reader.Icon.Clone();
                        }
                    }
                }
            } catch {
                pbIcon.Image = Utility.AppTypeIcon(AppPackageReader.getAppType(filePath));
            }
        }

        private async void AdbInstallForm_Shown(object sender, EventArgs e) {
            cancellationTokenSource = new CancellationTokenSource();
            await StartInstallationAsync(cancellationTokenSource.Token);
        }

        private async Task StartInstallationAsync(CancellationToken token) {
            isInstalling = true;
            btnAction.Text = Utility.GetResourceString("btnCancel", "Cancel");
            AppendLog("Starting ADB installation process...");

            string adbPath = FindAdbPath();
            if (string.IsNullOrEmpty(adbPath) || !File.Exists(adbPath)) {
                SetStatus(Utility.GetResourceString("strAdbNotFound", "ADB tool (adb.exe) was not found. Please install Android Platform Tools."), Color.Red);
                AppendLog("[ERROR] adb.exe not found on PATH or system folders.");
                FinishInstallation(false);
                return;
            }

            AppendLog("Using ADB path: " + adbPath);

            // Step 1: Check ADB Devices
            SetStatus("Checking connected Android devices...", Color.DarkBlue);
            string deviceOutput = await RunProcessAsync(adbPath, "devices", token);

            string deviceId = GetActiveDeviceId(deviceOutput, out string deviceState);

            if (string.IsNullOrEmpty(deviceId)) {
                if (deviceState == "unauthorized") {
                    SetStatus(Utility.GetResourceString("strAdbUnauthorized", "Device unauthorized! Please check your phone screen and allow RSA USB debugging permission."), Color.DarkOrange);
                } else if (deviceState == "offline") {
                    SetStatus(Utility.GetResourceString("strAdbOffline", "Device is offline. Please reconnect your USB cable."), Color.DarkOrange);
                } else {
                    SetStatus(Utility.GetResourceString("strAdbNoDevice", "No Android device connected. Please connect a device via USB with USB Debugging enabled."), Color.Red);
                }
                FinishInstallation(false);
                return;
            }

            AppendLog($"Target Device: {deviceId} [{deviceState}]");

            // Query Device Supported ABIs
            string deviceAbisOutput = await RunProcessAsync(adbPath, $"-s {deviceId} shell getprop ro.product.cpu.abilist", token);
            if (string.IsNullOrWhiteSpace(deviceAbisOutput)) {
                deviceAbisOutput = await RunProcessAsync(adbPath, $"-s {deviceId} shell getprop ro.product.cpu.abi", token);
            }
            string[] deviceAbis = deviceAbisOutput.Split(new[] { ',', '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            AppendLog("Device Supported ABIs: " + string.Join(", ", deviceAbis));

            // Step 2: Prepare Package Installation
            string ext = Path.GetExtension(filePath).ToLower();
            string tempDir = null;

            try {
                bool success = false;
                string lastErrorOutput = "";

                if (ext == ".xapk" || ext == ".apks" || ext == ".apkm") {
                    SetStatus("Extracting composite package (" + ext.ToUpper().TrimStart('.') + ")...", Color.DarkBlue);
                    tempDir = Path.Combine(Path.GetTempPath(), "ApkShellext_Xapk_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempDir);

                    ICSharpCode.SharpZipLib.Zip.FastZip fastZip = new ICSharpCode.SharpZipLib.Zip.FastZip();
                    fastZip.ExtractZip(filePath, tempDir, null);
                    var allApkFiles = Directory.GetFiles(tempDir, "*.apk", SearchOption.AllDirectories);

                    if (allApkFiles.Length == 0) {
                        SetStatus("Invalid XAPK: No APK files found inside container.", Color.Red);
                        FinishInstallation(false);
                        return;
                    }

                    // Attempt 1: All non-ABI splits + matching ABI split (if match found)
                    List<string> apksToInstall = FilterXapkApks(allApkFiles, deviceAbis, includeAbi: true);
                    SetStatus(Utility.GetResourceString("strAdbInstalling", "Installing package on device..."), Color.DarkBlue);

                    var res = await ExecuteAdbInstallAsync(adbPath, deviceId, apksToInstall, token);
                    success = res.Success;
                    lastErrorOutput = res.Output;

                    // Attempt 2: If Attempt 1 failed with ABI error, try non-ABI splits only
                    if (!success && (lastErrorOutput.Contains("NO_MATCHING_ABIS") || lastErrorOutput.Contains("MISSING_SPLIT") || lastErrorOutput.Contains("Failure"))) {
                        List<string> nonAbiApks = FilterXapkApks(allApkFiles, deviceAbis, includeAbi: false);
                        if (nonAbiApks.Count > 0 && nonAbiApks.Count != apksToInstall.Count) {
                            AppendLog("[FALLBACK 1] Retrying installation without incompatible ABI splits...");
                            SetStatus("Retrying XAPK installation (Architecture fallback)...", Color.DarkOrange);
                            res = await ExecuteAdbInstallAsync(adbPath, deviceId, nonAbiApks, token);
                            success = res.Success;
                            if (!success) lastErrorOutput = res.Output;
                        }
                    }

                    // Attempt 3: Standalone Base APK
                    if (!success) {
                        string baseApk = allApkFiles.FirstOrDefault(f => Path.GetFileName(f).ToLower().Contains("base") || !Path.GetFileName(f).ToLower().StartsWith("config.")) ?? allApkFiles[0];
                        AppendLog("[FALLBACK 2] Attempting standalone base APK installation: " + Path.GetFileName(baseApk));
                        SetStatus("Retrying fallback installation (Base APK)...", Color.DarkOrange);
                        res = await ExecuteAdbInstallAsync(adbPath, deviceId, new List<string> { baseApk }, token);
                        success = res.Success;
                        if (!success) lastErrorOutput = res.Output;
                    }
                } else {
                    SetStatus(Utility.GetResourceString("strAdbInstalling", "Installing package on device..."), Color.DarkBlue);
                    var res = await ExecuteAdbInstallAsync(adbPath, deviceId, new List<string> { filePath }, token);
                    success = res.Success;
                    lastErrorOutput = res.Output;
                }

                if (success) {
                    SetStatus("Success", Color.FromArgb(16, 124, 65));
                    progressBar.Style = ProgressBarStyle.Blocks;
                    progressBar.Value = 100;
                    FinishInstallation(true);
                } else {
                    string errLine = ExtractAdbErrorMessage(lastErrorOutput);
                    SetStatus("Installation Failed: " + errLine, Color.FromArgb(209, 52, 56));
                    FinishInstallation(false);
                }
            } catch (OperationCanceledException) {
                SetStatus(Utility.GetResourceString("strAdbCancelled", "Installation cancelled."), Color.DarkOrange);
                FinishInstallation(false);
            } catch (Exception ex) {
                string template = Utility.GetResourceString("strAdbFailed", "Installation failed: {0}");
                SetStatus(FormatErrorMsg(template, ex.Message), Color.Red);
                AppendLog("[EXCEPTION] " + ex.ToString());
                FinishInstallation(false);
            } finally {
                if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir)) {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        private async Task<AdbResult> ExecuteAdbInstallAsync(string adbPath, string deviceId, List<string> apks, CancellationToken token) {
            string installCmd;
            if (apks.Count == 1) {
                installCmd = $"-s {deviceId} install -r -g \"{apks[0]}\"";
            } else {
                string fileList = string.Join(" ", apks.Select(f => $"\"{f}\""));
                installCmd = $"-s {deviceId} install-multiple -r -g {fileList}";
            }

            AppendLog("Executing: adb " + installCmd);
            string result = await RunProcessAsync(adbPath, installCmd, token);

            bool isSuccess = result.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0;
            return new AdbResult { Success = isSuccess, Output = result };
        }

        private List<string> FilterXapkApks(string[] allApks, string[] deviceAbis, bool includeAbi) {
            List<string> selected = new List<string>();

            List<string> abiSplits = new List<string>();
            List<string> otherApks = new List<string>();

            foreach (var apk in allApks) {
                string name = Path.GetFileName(apk).ToLower();
                if (name.Contains("armeabi_v7a") || name.Contains("arm64_v8a") || name.Contains("x86_64") || name.Contains("x86")) {
                    abiSplits.Add(apk);
                } else {
                    otherApks.Add(apk);
                }
            }

            selected.AddRange(otherApks);

            if (includeAbi && abiSplits.Count > 0) {
                string bestAbiMatch = null;
                foreach (string devAbi in deviceAbis) {
                    string cleanDevAbi = devAbi.Replace("-", "_").ToLower();
                    bestAbiMatch = abiSplits.FirstOrDefault(a => Path.GetFileName(a).ToLower().Contains(cleanDevAbi));
                    if (bestAbiMatch != null) break;
                }

                if (bestAbiMatch != null) {
                    selected.Add(bestAbiMatch);
                    AppendLog("Selected matching ABI split: " + Path.GetFileName(bestAbiMatch));
                } else {
                    AppendLog("Notice: No exact matching ABI split found for device.");
                }
            }

            return selected;
        }

        private string FormatErrorMsg(string template, string detail) {
            if (string.IsNullOrWhiteSpace(detail)) detail = "Unknown error";
            detail = detail.Replace("{", "(").Replace("}", ")");
            if (template.Contains("{0}")) {
                try {
                    return string.Format(template, detail);
                } catch { }
            }
            return template + ": " + detail;
        }

        private string FindAdbPath() {
            // 1. Check C:\adb\adb.exe
            if (File.Exists(@"C:\adb\adb.exe")) return @"C:\adb\adb.exe";

            // 2. Check User Profile paths
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile)) {
                string p1 = Path.Combine(userProfile, "adb.exe");
                if (File.Exists(p1)) return p1;

                string p2 = Path.Combine(userProfile, @"platform-tools\adb.exe");
                if (File.Exists(p2)) return p2;

                string p3 = Path.Combine(userProfile, @"AppData\Local\Android\Sdk\platform-tools\adb.exe");
                if (File.Exists(p3)) return p3;
            }

            // 3. Check custom path from Settings
            string customPath = Utility.GetSetting("AdbPath");
            if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath)) {
                return customPath;
            }

            // 4. Check BaseDirectory (%~dp0adb.exe)
            string currentDirAdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb.exe");
            if (File.Exists(currentDirAdb)) return currentDirAdb;

            // 5. Check Program Files
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(progFiles)) {
                string p = Path.Combine(progFiles, @"Android\platform-tools\adb.exe");
                if (File.Exists(p)) return p;
            }
            string progFiles86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(progFiles86)) {
                string p = Path.Combine(progFiles86, @"Android\platform-tools\adb.exe");
                if (File.Exists(p)) return p;
            }

            // 6. Search PATH environment variable
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv)) {
                foreach (string p in pathEnv.Split(Path.PathSeparator)) {
                    try {
                        string full = Path.Combine(p.Trim(), "adb.exe");
                        if (File.Exists(full)) return full;
                    } catch { }
                }
            }

            return null;
        }

        private string GetActiveDeviceId(string output, out string state) {
            state = "none";
            if (string.IsNullOrEmpty(output)) return null;

            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines) {
                if (line.StartsWith("List of devices") || line.StartsWith("*")) continue;
                string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) {
                    string id = parts[0];
                    string status = parts[1];
                    state = status;
                    if (status == "device") {
                        return id;
                    }
                }
            }
            return null;
        }

        private string ExtractAdbErrorMessage(string output) {
            if (string.IsNullOrEmpty(output)) return "Unknown error occurred.";
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Priority 1: Extract inner Failure [REASON]
            foreach (string l in lines) {
                string line = l.Trim();
                if (line.Contains("Failure [")) {
                    int start = line.IndexOf("Failure [") + 9;
                    int end = line.LastIndexOf(']');
                    if (end > start) {
                        return line.Substring(start, end - start);
                    } else {
                        return line.Substring(start);
                    }
                }
            }

            // Priority 2: Extract INSTALL_FAILED_
            foreach (string l in lines) {
                string line = l.Trim();
                if (line.Contains("INSTALL_FAILED_")) {
                    int start = line.IndexOf("INSTALL_FAILED_");
                    return line.Substring(start).TrimEnd(']');
                }
            }

            // Priority 3: Extract last non-empty line without file path prefixes
            foreach (string l in lines.Reverse()) {
                string line = l.Trim();
                if (!line.StartsWith("List of") && !line.StartsWith("*") && !string.IsNullOrWhiteSpace(line)) {
                    if (line.Contains("failed to install")) {
                        int colIdx = line.LastIndexOf(':');
                        if (colIdx >= 0 && colIdx < line.Length - 1) {
                            return line.Substring(colIdx + 1).Trim();
                        }
                    }
                    return line;
                }
            }

            return "Installation failed.";
        }

        private Task<string> RunProcessAsync(string filename, string arguments, CancellationToken token) {
            return Task.Run(() => {
                StringBuilder sb = new StringBuilder();

                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = filename,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (Process process = new Process { StartInfo = psi }) {
                    currentProcess = process;

                    process.OutputDataReceived += (s, e) => {
                        if (e.Data != null) {
                            sb.AppendLine(e.Data);
                            AppendLog(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (s, e) => {
                        if (e.Data != null) {
                            sb.AppendLine(e.Data);
                            AppendLog(e.Data);
                        }
                    };

                    using (token.Register(() => {
                        try {
                            if (!process.HasExited) {
                                process.Kill();
                                KillAdbTask();
                            }
                        } catch { }
                    })) {
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        // Guarantee process exits and all redirected output streams flush
                        process.WaitForExit();
                    }

                    currentProcess = null;
                    return sb.ToString();
                }
            }, token);
        }

        private void KillAdbTask() {
            try {
                ProcessStartInfo psi = new ProcessStartInfo("taskkill.exe", "/F /IM adb.exe") {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            } catch { }
        }

        private void AppendLog(string message) {
            if (txtLog.InvokeRequired) {
                txtLog.Invoke(new Action(() => AppendLog(message)));
                return;
            }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private void SetStatus(string message, Color color) {
            if (lblStatus.InvokeRequired) {
                lblStatus.Invoke(new Action(() => SetStatus(message, color)));
                return;
            }
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
            UpdateLayoutPositions();
        }

        private void UpdateLayoutPositions() {
            if (lblStatus == null || txtLog == null || btnAction == null) return;
            lblStatus.MaximumSize = new Size(this.ClientSize.Width - 40, 0);
            int top = lblStatus.Bottom + 12;
            int availableHeight = btnAction.Top - top - 12;
            if (availableHeight > 50) {
                txtLog.Location = new Point(20, top);
                txtLog.Size = new Size(this.ClientSize.Width - 40, availableHeight);
            }
        }

        private void FinishInstallation(bool success) {
            if (this.InvokeRequired) {
                this.Invoke(new Action(() => FinishInstallation(success)));
                return;
            }
            isInstalling = false;
            progressBar.Style = ProgressBarStyle.Blocks;
            if (success) {
                progressBar.Value = 100;
            }
            btnAction.Text = Utility.GetResourceString("btnClose", "Close");
        }

        private void BtnAction_Click(object sender, EventArgs e) {
            if (isInstalling) {
                if (cancellationTokenSource != null) {
                    cancellationTokenSource.Cancel();
                }
                KillAdbTask();
                SetStatus(Utility.GetResourceString("strAdbCancelled", "Installation cancelled."), Color.DarkOrange);
                FinishInstallation(false);
            } else {
                this.Close();
            }
        }

        private void AdbInstallForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (isInstalling) {
                if (cancellationTokenSource != null) {
                    cancellationTokenSource.Cancel();
                }
                KillAdbTask();
            }
        }
    }
}
