using PlistCS;
using PNGDecrush;
using SharpShell.Attributes;
using SharpShell.Extensions;
using SharpShell.Diagnostics;
using SharpShell.Exceptions;
using SharpShell.ServerRegistration;
using SharpShell.SharpIconHandler;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using Ionic.Zlib;

namespace ApkShellext
{
    public class IpaReader : AppPackageReader
    {
        private string strAppRoot;
        private Dictionary<string, object> infoPlistDic;
        private Dictionary<string, object> itunesMetadataDic;
        private ZipFile zip;

        private const string iTunesMetadataPath = @"iTunesMetadata.plist";
        private const string infoPlistPath = @"(Payload/.*\.app/)Info\.plist";
        private const string CFBundleIcons = @"CFBundleIcons";
        private const string CFBundlePrimaryIcon = @"CFBundlePrimaryIcon";
        private const string CFBundleIconFile = @"CFBundleIconFile";
        private const string CFBundleIconFiles = @"CFBundleIconFiles";
        private const string CFBundleDisplayName = @"CFBundleDisplayName";
        private const string FacebookDisplayName = @"FacebookDisplayName";
        private const string CFBundleIdentifier = @"CFBundleIdentifier";
        private const string CFBundleShortVersionString = @"CFBundleShortVersionString";
        private const string CFBundleVersion = @"CFBundleVersion";
        private const string CFBundleResourceSpecification = @"CFBundleResourceSpecification";

        public const string flagAppId = @"itemId";
        public const string flagCopyright = @"copyright";

        public IpaReader(string path) {
            FileName = path;
            openStream(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        public IpaReader(Stream stream) {
            openStream(stream);
        }

        private void openStream(Stream stream) {
            zip = new ZipFile(stream);
            ZipEntry infoPlist = null;
            foreach (ZipEntry en in zip) {
                Match m = Regex.Match(en.Name, infoPlistPath, RegexOptions.IgnoreCase);
                if (m.Success) {
                    strAppRoot = m.Groups[1].Value;
                    infoPlist = en;
                    break;
                }
            }

            if (infoPlist == null) {
                // Fallback: look for any Info.plist in zip
                foreach (ZipEntry en in zip) {
                    if (en.Name.EndsWith("Info.plist", StringComparison.OrdinalIgnoreCase)) {
                        infoPlist = en;
                        int idx = en.Name.LastIndexOf("Info.plist", StringComparison.OrdinalIgnoreCase);
                        strAppRoot = en.Name.Substring(0, idx);
                        break;
                    }
                }
            }

            if (infoPlist == null) {
                throw new EntryPointNotFoundException("cannot find info.plist");
            }

            byte[] infoBytes;
            using (MemoryStream ms = new MemoryStream()) {
                using (Stream s = zip.GetInputStream(infoPlist)) {
                    s.CopyTo(ms);
                }
                infoBytes = ms.ToArray();
            }

            infoPlistDic = (Dictionary<string, object>)Plist.readPlist(infoBytes);
        }

        public string[] getStrings(Dictionary<string, object> dic, string[] keys) {
            if (dic == null || keys == null || keys.Length == 0) return new string[] { };
            for (int i = 0; i < keys.Length - 1; i++) {
                if (dic.ContainsKey(keys[i]) && dic[keys[i]] is Dictionary<string, object>) {
                    dic = (Dictionary<string, object>)dic[keys[i]];
                } else {
                    return new string[] { };
                }
            }
            if (dic.ContainsKey(keys[keys.Length - 1])) {
                object val = dic[keys[keys.Length - 1]];
                if (val is string sVal) {
                    return new string[] { sVal };
                }
                if (val is int || val is long) {
                    return new string[] { val.ToString() };
                }
                if (val is List<object> list) {
                    return Array.ConvertAll(list.ToArray(), x => x.ToString());
                }
            }
            return new string[] { };
        }

        public Bitmap getImage(string[] keys) {
            string[] images = getStrings(infoPlistDic, keys);
            if (images.Length > 0) {
                return getImage(images[0]);
            }
            return null;
        }

        public Bitmap getImage(string name) {
            if (string.IsNullOrEmpty(name) || zip == null) return null;
            string root = strAppRoot ?? "";
            ZipEntry image = null;

            if (!name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {                
                image = zip.GetEntry(root + name + @"@3x.png");                
                if (image == null) {
                    image = zip.GetEntry(root + name + @"@2x.png");
                }
                if (image == null) {
                    image = zip.GetEntry(root + name + ".png");
                }
            } else {
                image = zip.GetEntry(root + name);
            }

            if (image == null) {
                // Try case-insensitive lookup
                string target = (root + name).ToLower();
                foreach (ZipEntry en in zip) {
                    if (en.Name.ToLower().EndsWith(target) || en.Name.ToLower().EndsWith(target + ".png")) {
                        image = en;
                        break;
                    }
                }
            }

            if (image == null) {
                return null;
            }

            byte[] imageBytes;
            using (MemoryStream ms = new MemoryStream()) {
                using (Stream s = zip.GetInputStream(image)) {
                    s.CopyTo(ms);
                }
                imageBytes = ms.ToArray();
            }

            try {
                MemoryStream imageOut = new MemoryStream();
                PNGDecrusher.Decrush(new MemoryStream(imageBytes), imageOut);
                return new Bitmap(imageOut);
            } catch {
                return new Bitmap(new MemoryStream(imageBytes));
            }
        }

        public override AppPackageReader.AppType Type {
            get {
                return AppType.iOSApp;
            }
        }

        public override string AppName {
            get {
                try {
                    string[] n = getStrings(infoPlistDic, new string[] { CFBundleDisplayName });
                    if (n.Length == 0) {
                        n = getStrings(infoPlistDic, new string[] { FacebookDisplayName });
                    }
                    return n.Length > 0 ? n[0] : "";
                } catch {
                    return "";
                }
            }
        }

        public override string Version {
            get {
                try {
                    string[] v = getStrings(infoPlistDic, new string[] { CFBundleShortVersionString });
                    return v.Length > 0 ? v[0] : "";
                } catch {
                    return "";
                }
            }
        }

        public override string Revision {
            get {
                try {
                    string[] r = getStrings(infoPlistDic, new string[] { CFBundleVersion });
                    return r.Length > 0 ? r[0] : "";
                } catch {
                    return "";
                }
            }
        }

        public override string PackageName {
            get {
                try {
                    string[] p = getStrings(infoPlistDic, new string[] { CFBundleIdentifier });
                    return p.Length > 0 ? p[0] : "";
                } catch {
                    return "";
                }
            }
        }

        public override Bitmap Icon {
            get {
                try {
                    Bitmap icon = getImage(new string[] {
                        CFBundleIcons,
                        CFBundlePrimaryIcon,
                        CFBundleIconFiles });
                    if (icon == null) {
                        icon = getImage(new string[] {
                        CFBundleIcons,
                        CFBundlePrimaryIcon,
                        CFBundleIconFile });
                    }
                    if (icon == null) {
                        icon = getImage(new string[] { CFBundleIconFiles });
                    }
                    if (icon == null) {
                        icon = getImage(new string[] { CFBundleIconFile });
                    }
                    if (icon == null) {
                        icon = getImage("AppIcon");
                    }
                    if (icon == null) {
                        icon = getImage("Icon");
                    }
                    if (icon == null) {
                        // Fallback: search zip for any AppIcon or Icon PNG file
                        if (zip != null) {
                            foreach (ZipEntry en in zip) {
                                string enLower = en.Name.ToLower();
                                if ((enLower.Contains("appicon") || enLower.Contains("icon")) && enLower.EndsWith(".png")) {
                                    try {
                                        byte[] imageBytes;
                                        using (MemoryStream ms = new MemoryStream()) {
                                            using (Stream s = zip.GetInputStream(en)) {
                                                s.CopyTo(ms);
                                            }
                                            imageBytes = ms.ToArray();
                                        }
                                        MemoryStream imageOut = new MemoryStream();
                                        try {
                                            PNGDecrusher.Decrush(new MemoryStream(imageBytes), imageOut);
                                            return new Bitmap(imageOut);
                                        } catch {
                                            return new Bitmap(new MemoryStream(imageBytes));
                                        }
                                    } catch { }
                                }
                            }
                        }
                    }
                    return icon;
                } catch {
                    return null;
                }
            }
        }

        public override string Publisher {
            get {
                try {
                    ZipEntry itunesMetadata = zip.GetEntry(iTunesMetadataPath);
                    if (itunesMetadata == null)
                        return "";
                    byte[] itunesMetadataBytes = new byte[itunesMetadata.Size];
                    zip.GetInputStream(itunesMetadata).Read(itunesMetadataBytes, 0, (int)itunesMetadata.Size);
                    itunesMetadataDic = (Dictionary<string, object>)Plist.readPlist(itunesMetadataBytes);
                    return getStrings(itunesMetadataDic, new string[] { flagCopyright })[0];
                } catch {
                    return "";
                }
            }
        }

        public override string AppID {
            get {
                try {
                    ZipEntry itunesMetadata = zip.GetEntry(iTunesMetadataPath);
                    if (itunesMetadata == null)
                        return "";
                    byte[] itunesMetadataBytes = new byte[itunesMetadata.Size];
                    zip.GetInputStream(itunesMetadata).Read(itunesMetadataBytes, 0, (int)itunesMetadata.Size);
                    itunesMetadataDic = (Dictionary<string, object>)Plist.readPlist(itunesMetadataBytes);
                    return getStrings(itunesMetadataDic, new string[] { flagAppId })[0];
                } catch {
                    return "";
                }
            }
        }


        private bool disposed = false;
        protected override void Dispose(bool disposing) {
            if (disposed) return;
            if (disposing) {
                if (zip != null)
                    zip.Close();
            }
            disposed = true;
            base.Dispose(disposing);
        }

        public void Close() {
            Dispose(true);
        }

        ~IpaReader() {
            Dispose(true);
        }
    }
}
