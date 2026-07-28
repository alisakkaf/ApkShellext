using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Xml;
using System.Drawing;
using System.Text.RegularExpressions;

namespace ApkShellext {
    /// <summary>
    /// 
    /// </summary>
    public class AppxReader : AppPackageReader {
        private const string AppxManifestXml = @"AppxManifest.xml";
        //private const string elemPackage = @"Package";
        private const string elemIdentity = @"Identity";
        private const string elemProperties = @"Properties";
        private const string elemDisplayName = @"DisplayName";
        private const string elemLogo = @"Logo";
        private const string elemPhoneIdentity = @"mp:PhoneIdentity";

        private const string attrVersion = @"Version";
        private const string attrName = @"Name";
        private const string attrPublisher = @"Publisher";
        private const string attrPhoneProductID = @"PhoneProductId";

        private ZipFile zip;
        private string iconPath;

        private string version;
        private string appname;
        private string packageName;
        private string publisher;
        private string productid;

        public AppxReader(Stream stream) {
            FileName = "";
            zip = new ZipFile(stream);
            Extract();
        }

        public AppxReader(string path) {
            FileName = path;
            zip = new ZipFile(FileName);
            Extract();
        }

        public void Extract() {
            ZipEntry en = zip.GetEntry(AppxManifestXml);
            if (en == null)
                throw new EntryPointNotFoundException("cannot find " + AppxManifestXml);

            XmlDocument xml = new XmlDocument();
            xml.XmlResolver = null;
            using (Stream s = zip.GetInputStream(en)) {
                xml.Load(s);
            }

            XmlElement packageNode = xml.DocumentElement;
            XmlElement Identity = packageNode != null ? packageNode[elemIdentity] : null;
            if (Identity != null) {
                version = Identity.HasAttribute(attrVersion) ? Identity.GetAttribute(attrVersion) : "";
                packageName = Identity.HasAttribute(attrName) ? Identity.GetAttribute(attrName) : "";
                publisher = Identity.HasAttribute(attrPublisher) ? Identity.GetAttribute(attrPublisher) : "";
                Match m = Regex.Match(publisher, @"CN=([^,]*),?");
                if (m.Success) {
                    publisher = m.Groups[1].Value;
                }
            }

            XmlElement Properties = packageNode != null ? packageNode[elemProperties] : null;
            if (Properties != null) {
                XmlElement DisplayName = Properties[elemDisplayName];
                if (DisplayName != null && DisplayName.FirstChild != null) {
                    appname = DisplayName.FirstChild.Value;
                }
                XmlElement Logo = Properties[elemLogo];
                if (Logo != null && Logo.FirstChild != null) {
                    iconPath = Logo.FirstChild.Value.Replace(@"\", @"/");
                }
            }

            // Also search uap:VisualElements / VisualElements in Applications/Application for modern UWP apps
            if (string.IsNullOrEmpty(iconPath)) {
                XmlNodeList visualElementsList = xml.GetElementsByTagName("uap:VisualElements");
                if (visualElementsList.Count == 0) visualElementsList = xml.GetElementsByTagName("VisualElements");
                if (visualElementsList.Count > 0) {
                    XmlElement ve = (XmlElement)visualElementsList[0];
                    string[] logoAttrs = new string[] { "Square150x150Logo", "Square44x44Logo", "Square71x71Logo", "Square310x310Logo", "Logo" };
                    foreach (string attr in logoAttrs) {
                        if (ve.HasAttribute(attr)) {
                            iconPath = ve.GetAttribute(attr).Replace(@"\", @"/");
                            break;
                        }
                    }
                }
            }

            XmlElement PhoneIdentity = packageNode != null ? packageNode[elemPhoneIdentity] : null;
            if (PhoneIdentity != null && PhoneIdentity.HasAttribute(attrPhoneProductID)) {
                productid = PhoneIdentity.GetAttribute(attrPhoneProductID);
            } else {
                productid = packageName;
            }
        }

        public override AppPackageReader.AppType Type {
            get {
                return AppType.WindowsPhoneApp;
            }
        }

        public override string AppName {
            get {
                return !string.IsNullOrEmpty(appname) ? appname : packageName;
            }
        }

        public override string Version {
            get {
                return version ?? "";
            }
        }

        public override string Publisher {
            get {
                return publisher ?? "";
            }
        }

        public override string PackageName {
            get {
                return packageName ?? "";
            }
        }

        public override string AppID {
            get {
                return productid ?? "";
            }
        }

        public override Bitmap Icon {
            get {
                if (!string.IsNullOrEmpty(iconPath)) {
                    int dot = iconPath.LastIndexOf(".");
                    string name = dot > 0 ? iconPath.Substring(0, dot) : iconPath;
                    string extension = dot > 0 ? iconPath.Substring(dot + 1) : "png";
                    ZipEntry logo = null;
                    int scale = -1;
                    foreach (ZipEntry en in zip) {
                        Match m = Regex.Match(en.Name, Regex.Escape(name) + @"(\.scale\-(\d+)|targetsize\-(\d+))?" + @"\." + Regex.Escape(extension), RegexOptions.IgnoreCase);
                        if (m.Success) {
                            if (m.Groups[1].Value == "") {
                                logo = en;
                                break;
                            } else {
                                int newScale = 0;
                                if (m.Groups[2].Value != "") int.TryParse(m.Groups[2].Value, out newScale);
                                else if (m.Groups[3].Value != "") int.TryParse(m.Groups[3].Value, out newScale);
                                if (newScale > scale) {
                                    logo = en;
                                    scale = newScale;
                                }
                            }
                        }
                    }
                    if (logo == null) {
                        logo = zip.GetEntry(iconPath);
                    }
                    if (logo != null) {
                        try {
                            using (MemoryStream ms = new MemoryStream()) {
                                using (Stream s = zip.GetInputStream(logo)) {
                                    s.CopyTo(ms);
                                }
                                ms.Position = 0;
                                return new Bitmap(ms);
                            }
                        } catch { }
                    }
                }

                // Fallback: search zip for any logo/store PNG or JPG image
                if (zip != null) {
                    foreach (ZipEntry en in zip) {
                        string nameLower = en.Name.ToLower();
                        if ((nameLower.Contains("logo") || nameLower.Contains("store") || nameLower.Contains("icon")) && (nameLower.EndsWith(".png") || nameLower.EndsWith(".jpg"))) {
                            try {
                                using (MemoryStream ms = new MemoryStream()) {
                                    using (Stream s = zip.GetInputStream(en)) {
                                        s.CopyTo(ms);
                                    }
                                    ms.Position = 0;
                                    return new Bitmap(ms);
                                }
                            } catch { }
                        }
                    }
                }
                return null;
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

        ~AppxReader() {
            Dispose(true);
        }

    }
}
