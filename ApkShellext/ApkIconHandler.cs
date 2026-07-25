using ApkQuickReader;
using Microsoft.Win32;
using SharpShell.Attributes;
using SharpShell.Diagnostics;
using SharpShell.Extensions;
using SharpShell.ServerRegistration;
using SharpShell.SharpIconHandler;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ApkShellext.Properties;
using System.Threading;
using System.Text.RegularExpressions;

namespace ApkShellext {
    /// <summary>
    /// This is the icon handler, not only supporting apk, but also other type of apps.
    /// </summary>
    [Guid("6F1D5E99-4A9B-4D4F-8C8B-8D75A0B1A7DF")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".apk")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".xapk")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".ipa")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".appxbundle")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".appx")]
    public class ApkIconHandler : SharpIconHandler {
        private Bitmap m_icon = null;
        protected override Icon GetIcon(bool smallIcon, uint iconSize) {
            if (m_icon == null) {
                try {
                    AppPackageReader.AppType appType = AppPackageReader.getAppType(SelectedItemPath);
                    var cached = PackageCache.Get(SelectedItemPath);

                    if (cached != null) {
                        if (cached.Type == AppPackageReader.AppType.iOSApp && Utility.GetSetting("ShowIpaIcon", "True") != "True") {
                            m_icon = Utility.AppTypeIcon(cached.Type);
                        } else if ((cached.Type == AppPackageReader.AppType.WindowsPhoneApp || cached.Type == AppPackageReader.AppType.WindowsPhoneAppBundle) && Utility.GetSetting("ShowAppxIcon", "False") != "True") {
                            m_icon = Utility.AppTypeIcon(cached.Type);
                        } else {
                            m_icon = cached.GetIconClone(new Size((int)iconSize, (int)iconSize));
                        }
                    } else {
                        using (AppPackageReader reader = AppPackageReader.Read(SelectedItemPath)) {
                            if (reader != null) {
                                PackageCache.Put(SelectedItemPath, reader);

                                if (reader.Type == AppPackageReader.AppType.iOSApp && Utility.GetSetting("ShowIpaIcon", "True") != "True") {
                                    m_icon = Utility.AppTypeIcon(reader.Type);
                                } else if ((reader.Type == AppPackageReader.AppType.WindowsPhoneApp || reader.Type == AppPackageReader.AppType.WindowsPhoneAppBundle) && Utility.GetSetting("ShowAppxIcon", "False") != "True") {
                                    m_icon = Utility.AppTypeIcon(reader.Type);
                                } else {
                                    m_icon = reader.getIcon(new Size((int)iconSize, (int)iconSize));
                                }
                            }
                        }
                    }

                    if (m_icon == null)
                        throw new Exception("Cannot find Icon for " + Path.GetFileName(SelectedItemPath) + ", draw default");

                    if (Utility.GetSetting("ShowOverLayIcon") == "True") {
                        Bitmap tempOverlay = Utility.CombineBitmap(m_icon,
                           Utility.AppTypeIcon(appType),
                           new Rectangle((int)(m_icon.Width * 0.05), 0, (int)(m_icon.Width * 0.95), (int)(m_icon.Height * 0.95)),
                           new Rectangle(0, (int)m_icon.Height / 2, (int)m_icon.Width / 2, (int)m_icon.Height / 2),
                           new Size((int)m_icon.Width, (int)m_icon.Height));
                        m_icon.Dispose();
                        m_icon = tempOverlay;
                    }
                } catch (Exception ex) {
                    Log("Error in reading icon for " + Path.GetFileName(SelectedItemPath) + ", draw default : " + ex.Message);
                    m_icon = Utility.AppTypeIcon(AppPackageReader.getAppType(SelectedItemPath));
                }
            }
            Bitmap resizedIcon = Utility.ResizeBitmap(m_icon, new Size((int)iconSize, (int)iconSize));
            if (iconSize >= 32 && Utility.GetSetting("ShowAliSakkaFWatermark", "False") == "True") {
                try {
                    using (Graphics g = Graphics.FromImage(resizedIcon)) {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        
                        string text = "By AliSakkaF";
                        float fontSize = 5.0f + (iconSize / 24f);
                        if (fontSize < 4.5f) fontSize = 4.5f;
                        
                        // Measure and adapt the font size so it fits perfectly (max 70% of the icon width)
                        float maxTextWidth = iconSize * 0.70f;
                        using (Font tempFont = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Point)) {
                            SizeF measuredSize = g.MeasureString(text, tempFont);
                            if (measuredSize.Width > maxTextWidth) {
                                fontSize = fontSize * (maxTextWidth / measuredSize.Width);
                                if (fontSize < 4.0f) fontSize = 4.0f; // minimum size
                            }
                        }

                        using (Font font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Point)) {
                            SizeF textSize = g.MeasureString(text, font);
                            float x = resizedIcon.Width - textSize.Width - 1.5f;
                            float y = resizedIcon.Height - textSize.Height - 1f;
                            
                            // Draw 8-directional outline in black for high contrast
                            using (Brush shadowBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0))) {
                                float offset = Math.Max(0.5f, fontSize / 10f); // dynamic shadow offset based on font size
                                for (int dx = -1; dx <= 1; dx++) {
                                    for (int dy = -1; dy <= 1; dy++) {
                                        if (dx != 0 || dy != 0) {
                                            g.DrawString(text, font, shadowBrush, x + dx * offset, y + dy * offset);
                                        }
                                    }
                                }
                            }
                            // Draw foreground clean white text
                            using (Brush textBrush = new SolidBrush(Color.White)) {
                                g.DrawString(text, font, textBrush, x, y);
                            }
                        }
                    }
                } catch { }
            }
            IntPtr hIcon = resizedIcon.GetHicon();
            try {
                using (Icon tempIcon = Icon.FromHandle(hIcon)) {
                    return (Icon)tempIcon.Clone();
                }
            } finally {
                DestroyIcon(hIcon);
                resizedIcon.Dispose();
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [CustomRegisterFunction]
        public static void postDoRegister(Type type, RegistrationType registrationType) {
            Console.WriteLine("Registering " + type.FullName + " Version " + type.Assembly.GetName().Version.ToString());

            // Todo: clean other icon handler as they were integrated
            //"d5ff6172-1ae5-4c4a-a207-5a2dd100891e"
            //"a0ac4758-12d3-4dcf-9d12-03faaa3c0a9d"

            #region Clean up apkshellext registry
            try {
                string oldVersionClassName = @"KKHomeProj.ApkShellExt.ApkShellExt";
                string oldVersionCLID = "";
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(oldVersionClassName + @"\CLSID")) {
                    if (key != null) {
                        Console.WriteLine("Found old version in registry, cleaning up ...");
                        oldVersionCLID = (string)key.GetValue(null);
                        key.Close();
                        Registry.ClassesRoot.DeleteSubKeyTree(oldVersionClassName);
                    }
                }
                if (oldVersionCLID != "") {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\CLSID\" + oldVersionCLID)) {
                        if (key != null) {
                            key.Close();
                            Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Classes\CLSID\" + oldVersionCLID);
                        }
                    }
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved\" + oldVersionCLID)) {
                        if (key != null) {
                            key.Close();
                            Registry.LocalMachine.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved\" + oldVersionCLID);
                        }
                    }
                }
            } catch (Exception e) {
                Logging.Error("Cleaning up apkshellext remaining registry items but see exception" +
                    e.Message);
            }
            #endregion

            #region Clean up older versions registry
            try {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"\CLSID\" +
                    type.GUID.ToRegistryString() + @"\InprocServer32")) {
                    if (key != null && key.GetSubKeyNames().Count() != 0) {
                        Console.WriteLine("Found old version in registry, cleaning up ...");
                        foreach (var k in key.GetSubKeyNames()) {
                            if (k != type.Assembly.GetName().Version.ToString()) {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"\CLSID\" +
                        type.GUID.ToRegistryString() + @"\InprocServer32\" + k);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Logging.Error("Cleaning up older version but see exception. "
                     + e.Message);
            }

            #endregion

            #region restore the old settings from registry
            // This is not possible as at this time, the user is admin, which is different when running explorer, so comment this out for now.
            //Settings.Default.ShowAppStoreWhenMultiSelected = Utility.getRegistrySetting(Utility.keyMultiSelectShowStore) == 1;
            //Settings.Default.ShowGooglePlay = Utility.getRegistrySetting(Utility.keyShowGooglePlay) == 1;
            //Settings.Default.ShowOverLayIcon = Utility.getRegistrySetting(Utility.keyShowOverlay) == 1;
            //Settings.Default.ShowIpaIcon = Utility.getRegistrySetting(Utility.keyShowIpaIcon) == 1;
            //Settings.Default.ShowAppxIcon = Utility.getRegistrySetting(Utility.keyShowAppxIcon) == 1;
            //Settings.Default.ShowMenuIcon = Utility.getRegistrySetting(Utility.keyShowMenuIcon) == 1;

            //if (Utility.getRegistrySetting(Utility.keyRenameWithVersionCode) == 1) {
            //    Settings.Default.RenamePattern = Resources.strRenamePatternDefault + "_" + Resources.varRevision;
            //}

            //int lang = Utility.getRegistrySetting(Utility.keyLanguage, -1);
            //if (lang != -1) {
            //    Settings.Default.Language = lang;
            //}
            //Settings.Default.Save();
            #endregion

            #region Enable debug log
#if DEBUG
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE", true).CreateSubKey(@"SharpShell", RegistryKeyPermissionCheck.ReadWriteSubTree)) {
                key.SetValue("LoggingMode", 4);
                key.SetValue("LogPath", @"%AppData%\apkshellext.log",RegistryValueKind.ExpandString);
            }
#endif
            #endregion
        }

        protected override void Log(string message) {
            Utility.Log(this, Path.GetFileName(SelectedItemPath), message);
        }
    }
}
