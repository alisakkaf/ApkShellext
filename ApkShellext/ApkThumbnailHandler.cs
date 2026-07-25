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

namespace ApkShellext {
    [Guid("7E5E98B4-9F8C-476A-B6A2-1C3D6E9A8C5B")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".apk")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".xapk")]
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

                if (SelectedItemStream != null && SelectedItemStream.CanSeek) {
                    SelectedItemStream.Position = 0;
                    AppPackageReader reader = null;
                    try {
                        reader = new ApkReader(SelectedItemStream);
                    } catch {
                        try {
                            SelectedItemStream.Position = 0;
                            reader = new XapkReader(SelectedItemStream);
                        } catch {
                            try {
                                SelectedItemStream.Position = 0;
                                reader = new IpaReader(SelectedItemStream);
                            } catch {
                                try {
                                    SelectedItemStream.Position = 0;
                                    reader = new AppxBundleReader(SelectedItemStream);
                                } catch {
                                    SelectedItemStream.Position = 0;
                                    reader = new AppxReader(SelectedItemStream);
                                }
                            }
                        }
                    }

                    using (reader) {
                        if (reader != null) {
                            rawIcon = reader.Icon;
                        }
                    }
                }

                if (rawIcon == null)
                    throw new Exception("Cannot find Icon from Stream");

                if (rawIcon.Height < outputSize && Utility.GetSetting("StretchThumbnail", "True") != "True")
                    outputSize = rawIcon.Height;

                Bitmap finalBitmap = null;
                if (Utility.GetSetting("ShowOverlayIcon") == "True") {
                    finalBitmap = Utility.CombineBitmap(rawIcon,
                           Utility.AppTypeIcon(AppPackageReader.AppType.AndroidApp),
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

            string[] exts = new string[] { ".apk", ".xapk", ".ipa", ".appx", ".appxbundle" };
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
