using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Drawing;
using ApkQuickReader;

namespace ApkShellext
{
    /// <summary>
    /// XapkReader extracts the base APK from a .xapk, .apks, or .apkm ZIP package
    /// and delegates all APK parsing to the base ApkReader.
    /// </summary>
    public class XapkReader : ApkReader
    {
        public XapkReader(string filename, string culture = "") 
            : base(GetXapkStream(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), culture)
        {
        }

        public XapkReader(Stream stream, string culture = "") 
            : base(GetXapkStream(stream), culture)
        {
        }

        private static Stream GetXapkStream(Stream xapkStream)
        {
            ZipFile zFile = null;
            string tempFilePath = null;
            try
            {
                zFile = new ZipFile(xapkStream);
                zFile.IsStreamOwner = false;

                ZipEntry bestApkEntry = null;
                int highestScore = -1;

                // Search and score all APK entries inside the bundle (.xapk, .apks, .apkm)
                foreach (ZipEntry entry in zFile)
                {
                    if (!entry.IsFile) continue;
                    string entryName = entry.Name.Replace('\\', '/');
                    string fileName = Path.GetFileName(entryName).ToLower();

                    if (fileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                    {
                        int score = 0;
                        if (fileName == "base.apk") score = 1000;
                        else if (entryName.EndsWith("/base.apk", StringComparison.OrdinalIgnoreCase)) score = 950;
                        else if (fileName == "base-master.apk") score = 900;
                        else if (fileName == "standalone.apk") score = 850;
                        else if (fileName == "main.apk") score = 800;
                        else if (!fileName.StartsWith("split_") && !fileName.Contains("config.")) score = 500;
                        else score = 100;

                        if (score > highestScore)
                        {
                            highestScore = score;
                            bestApkEntry = entry;
                        }
                    }
                }

                if (bestApkEntry == null)
                {
                    throw new FileNotFoundException("Could not find any base APK inside package container.");
                }

                // Extract base APK into a temp file to avoid huge memory allocations for 300MB+ APKs
                tempFilePath = Path.Combine(Path.GetTempPath(), "ApkShellext_" + Guid.NewGuid().ToString("N") + ".apk");
                using (Stream apkStream = zFile.GetInputStream(bestApkEntry))
                using (FileStream tempFs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    apkStream.CopyTo(tempFs);
                }

                FileStream streamToRead = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

                return new ReleaseZipStream(streamToRead, zFile, tempFilePath);
            }
            catch
            {
                if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
                if (zFile != null)
                {
                    try { zFile.Close(); } catch { }
                }
                throw;
            }
        }
    }

    /// <summary>
    /// Custom Stream wrapper that automatically disposes the parent ZipFile and deletes 
    /// the temporary APK file when the inner APK stream is disposed.
    /// </summary>
    public class ReleaseZipStream : Stream
    {
        private Stream innerStream;
        private ZipFile zipFile;
        private string tempFilePath;

        public ReleaseZipStream(Stream innerStream, ZipFile zipFile, string tempFilePath = null)
        {
            this.innerStream = innerStream;
            this.zipFile = zipFile;
            this.tempFilePath = tempFilePath;
        }

        public override bool CanRead => innerStream != null && innerStream.CanRead;
        public override bool CanSeek => innerStream != null && innerStream.CanSeek;
        public override bool CanWrite => innerStream != null && innerStream.CanWrite;
        public override long Length => innerStream != null ? innerStream.Length : 0;
        public override long Position
        {
            get => innerStream != null ? innerStream.Position : 0;
            set { if (innerStream != null) innerStream.Position = value; }
        }

        public override void Flush() => innerStream?.Flush();
        public override int Read(byte[] buffer, int offset, int count) => innerStream != null ? innerStream.Read(buffer, offset, count) : 0;
        public override long Seek(long offset, SeekOrigin origin) => innerStream != null ? innerStream.Seek(offset, origin) : 0;
        public override void SetLength(long value) => innerStream?.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => innerStream?.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (innerStream != null)
                {
                    try { innerStream.Dispose(); } catch { }
                    innerStream = null;
                }
                if (zipFile != null)
                {
                    try { zipFile.Close(); } catch { }
                    zipFile = null;
                }
                if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
            }
            base.Dispose(disposing);
        }
    }
}
