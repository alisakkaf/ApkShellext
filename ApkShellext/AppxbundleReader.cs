using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using System.Xml;
using System.IO;
using System.Drawing;

namespace ApkShellext {
    /// <summary>
    /// Read AppxBundle
    /// </summary>
    public class AppxBundleReader :AppPackageReader {
        private ZipFile zip;
        private AppxReader appxReader= null;

        private const string AppxBundleManifestXml = @"AppxMetadata/AppxBundleManifest.xml";
        private const string ElemIdentity = @"Identity";
        //private const string ElemProperties = @"Properties";
        //private const string ElemDisplayName = @"DisplayName";
        private const string ElemPackage = @"Package";
        //private const string AttrVersion = @"Version";
        private const string AttrName = @"Name";
        //private const string AttrPublisher = @"Publisher";
        private const string AttrType = @"Type";
        private const string ValApplication = @"application";
        private const string AttrFileName = @"FileName";

        public AppxBundleReader(Stream stream) {
            FileName = "";
            openFile(stream);
        }

        public AppxBundleReader(string path) {
            FileName = path;
            openFile(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        private void openFile(Stream stream) {
            string appxFileName = "";
            zip = new ZipFile(stream);
            ZipEntry en = zip.GetEntry(AppxBundleManifestXml);

            if (en != null) {
                try {
                    using (XmlReader reader = XmlReader.Create(zip.GetInputStream(en))) {
                        reader.ReadToFollowing(ElemIdentity);
                        reader.MoveToAttribute(AttrName);

                        do {
                            if (reader.ReadToFollowing(ElemPackage)) {
                                reader.MoveToAttribute(AttrType);
                            }
                        } while (!reader.EOF && reader.Value != ValApplication);

                        if (!reader.EOF) {
                            reader.MoveToAttribute(AttrFileName);
                            appxFileName = reader.Value;
                        }
                    }
                } catch { }
            }

            if (string.IsNullOrEmpty(appxFileName)) {
                // Fallback: search for any .appx entry in the bundle zip
                foreach (ZipEntry entry in zip) {
                    if (entry.Name.EndsWith(".appx", StringComparison.OrdinalIgnoreCase)) {
                        en = entry;
                        appxFileName = entry.Name;
                        break;
                    }
                }
            } else {
                en = zip.GetEntry(appxFileName);
            }

            if (en == null)
                throw new EntryPointNotFoundException("cannot find appx " + appxFileName);

            // Copy inner .appx into a seekable MemoryStream so ZipFile inside AppxReader can parse it properly
            MemoryStream ms = new MemoryStream();
            using (Stream s = zip.GetInputStream(en)) {
                s.CopyTo(ms);
            }
            ms.Position = 0;
            appxReader = new AppxReader(ms);
        }

        public override AppPackageReader.AppType Type {
            get {
                return AppType.WindowsPhoneAppBundle;
            }
        }

        public override Bitmap Icon {
            get {
                return appxReader.Icon;
            }
        }

        public override string AppName {
            get {
                return appxReader.AppName;
            }
        }

        public override string PackageName {
            get {
                return appxReader.PackageName;
            }
        }

        public override string Version {
            get {
                return appxReader.Version;
            }
        }

        public override string Publisher {
            get {
                return appxReader.Publisher;
            }
        }

        public override string AppID {
            get {
                return appxReader.AppID;
            }
        }
        private bool disposed = false;
        protected override void Dispose(bool disposing) {
            if (disposed) return;
            if (disposing) {
                if (appxReader != null) {
                    appxReader.Close();
                }
                if (zip != null)
                    zip.Close();
            }
            disposed = true;
            base.Dispose(disposing);
        }

        public void Close() {
            Dispose(true);
        }

        ~AppxBundleReader() {
            Dispose(true);
        }
    }
}
