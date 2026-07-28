using Microsoft.Win32;
using SharpShell.Attributes;
using SharpShell.Diagnostics;
using SharpShell.SharpThumbnailHandler;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using SharpShell.Extensions;
using SharpShell.ServerRegistration;
using System.IO;
using ApkShellext.Properties;
using ApkQuickReader;
using System.Configuration;
using ICSharpCode.SharpZipLib.Zip;

namespace ApkShellext {
    [Guid("7E5E98B4-9F8C-476A-B6A2-1C3D6E9A8C5B")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".apk")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".xapk")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".apks")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".apkm")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".ipa")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".appxbundle")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".appx")]
    public class ApkThumbnailHandler : SharpThumbnailHandler {
        protected override Bitmap GetThumbnailImage(uint width) {
            if (Utility.GetSetting("EnableThumbnail") != "True") {
                return null;
            }

            try {
                int outputSize = (int)width;
                Bitmap rawIcon = null;
                AppPackageReader.AppType appType = AppPackageReader.AppType.AndroidApp;

                if (SelectedItemStream != null && SelectedItemStream.CanSeek) {
                    string detectedType = null;
                    try {
                        SelectedItemStream.Position = 0;
                        using (ZipFile zipCheck = new ZipFile(SelectedItemStream)) {
                            zipCheck.IsStreamOwner = false; // CRITICAL: Do NOT close SelectedItemStream when zipCheck is disposed!
                            foreach (ZipEntry entry in zipCheck) {
                                string nameLower = entry.Name.ToLower();
                                if (nameLower == "androidmanifest.xml") {
                                    detectedType = "apk";
                                    break;
                                } else if (nameLower == "appxmanifest.xml") {
                                    detectedType = "appx";
                                    break;
                                } else if (nameLower == "appxmetadata/appxbundlemanifest.xml") {
                                    detectedType = "appxbundle";
                                    break;
                                } else if (nameLower.EndsWith(".apk") || nameLower == "manifest.json" || nameLower == "info.json") {
                                    if (detectedType == null) detectedType = "xapk";
                                } else if (nameLower.Contains("info.plist")) {
                                    if (detectedType == null) detectedType = "ipa";
                                }
                            }
                        }
                    } catch { }

                    AppPackageReader reader = null;

                    if (detectedType == "apk") {
                        try { SelectedItemStream.Position = 0; reader = new ApkReader(SelectedItemStream); } catch { }
                    } else if (detectedType == "xapk") {
                        try { SelectedItemStream.Position = 0; reader = new XapkReader(SelectedItemStream); } catch { }
                    } else if (detectedType == "ipa") {
                        try { SelectedItemStream.Position = 0; reader = new IpaReader(SelectedItemStream); } catch { }
                    } else if (detectedType == "appxbundle") {
                        try { SelectedItemStream.Position = 0; reader = new AppxBundleReader(SelectedItemStream); } catch { }
                    } else if (detectedType == "appx") {
                        try { SelectedItemStream.Position = 0; reader = new AppxReader(SelectedItemStream); } catch { }
                    }

                    // Fallback trial chain if detection returned null
                    if (reader == null) {
                        try { SelectedItemStream.Position = 0; reader = new ApkReader(SelectedItemStream); } catch { }
                    }
                    if (reader == null) {
                        try { SelectedItemStream.Position = 0; reader = new XapkReader(SelectedItemStream); } catch { }
                    }
                    if (reader == null) {
                        try { SelectedItemStream.Position = 0; reader = new IpaReader(SelectedItemStream); } catch { }
                    }
                    if (reader == null) {
                        try { SelectedItemStream.Position = 0; reader = new AppxBundleReader(SelectedItemStream); } catch { }
                    }
                    if (reader == null) {
                        try { SelectedItemStream.Position = 0; reader = new AppxReader(SelectedItemStream); } catch { }
                    }

                    if (reader != null) {
                        using (reader) {
                            appType = reader.Type;
                            rawIcon = reader.Icon;
                        }
                    }
                }

                if (rawIcon == null)
                    throw new Exception("Cannot find Icon from Stream");

                if (rawIcon.Height < outputSize && Utility.GetSetting("StretchThumbnail", "True") != "True")
                    outputSize = rawIcon.Height;

                Bitmap finalBitmap = null;
                if (Utility.GetSetting("ShowOverLayIcon") == "True" || Utility.GetSetting("ShowOverlayIcon") == "True") {
                    finalBitmap = Utility.CombineBitmap(rawIcon,
                           Utility.AppTypeIcon(appType),
                           new Rectangle(0, 0, outputSize, outputSize),
                           new Rectangle(0, outputSize / 2, outputSize / 2, outputSize / 2),
                           new Size(outputSize, outputSize));
                    rawIcon.Dispose();
                } else {
                    finalBitmap = Utility.ResizeBitmap(rawIcon, new Size(outputSize, outputSize));
                    if (finalBitmap != rawIcon) {
                        rawIcon.Dispose();
                    }
                }
                return finalBitmap;
            } catch (Exception ex) {
                Log("Error in reading thumbnail icon: " + ex.Message);
                return null;
            }
        }

        [CustomRegisterFunction]
        public static void postDoRegister(Type type, RegistrationType registrationType) {
            Console.WriteLine("Registering " + type.FullName );

            string[] exts = new string[] { ".apk", ".xapk", ".apks", ".apkm", ".ipa", ".appx", ".appxbundle" };
            foreach (string ext in exts) {
                try {
                    using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"\CLSID\" + ext)) {
                        if (key != null) {
                            key.SetValue("Treatment", 0);
                        }
                    }
                } catch { }
            }

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
        }

        protected override void Log(string message) {
            Utility.Log(this, "", message);
        }
    }
}
