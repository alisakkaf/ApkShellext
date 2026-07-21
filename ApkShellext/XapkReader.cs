using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Drawing;
using ApkQuickReader;

namespace ApkShellext
{
    /// <summary>
    /// XapkReader extracts the base APK from a .xapk ZIP package (via file path or stream)
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
            try
            {
                zFile = new ZipFile(xapkStream);
                ZipEntry baseApkEntry = null;

                // Search for the main APK file (normally base.apk)
                foreach (ZipEntry entry in zFile)
                {
                    if (entry.Name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                    {
                        // Prefer base.apk, otherwise take the first .apk found
                        if (entry.Name.Equals("base.apk", StringComparison.OrdinalIgnoreCase))
                        {
                            baseApkEntry = entry;
                            break;
                        }
                        if (baseApkEntry == null)
                        {
                            baseApkEntry = entry;
                        }
                    }
                }

                if (baseApkEntry == null)
                {
                    throw new FileNotFoundException("Could not find any base APK inside XAPK package.");
                }

                // Copy the base APK into a MemoryStream to prevent file lock issues
                Stream apkStream = zFile.GetInputStream(baseApkEntry);
                MemoryStream ms = new MemoryStream();
                apkStream.CopyTo(ms);
                ms.Position = 0;

                // Return our custom wrapper stream that owns the ZipFile and MemoryStream.
                // When this stream is disposed, it will dispose both the MemoryStream and the ZipFile.
                return new ReleaseZipStream(ms, zFile);
            }
            catch
            {
                if (zFile != null)
                {
                    try { zFile.Close(); } catch {}
                }
                throw;
            }
        }
    }

    /// <summary>
    /// Custom Stream wrapper that automatically closes and disposes the parent ZipFile 
    /// when the inner APK stream is disposed.
    /// </summary>
    public class ReleaseZipStream : Stream
    {
        private Stream innerStream;
        private ZipFile zipFile;

        public ReleaseZipStream(Stream innerStream, ZipFile zipFile)
        {
            this.innerStream = innerStream;
            this.zipFile = zipFile;
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
                    innerStream.Dispose();
                    innerStream = null;
                }
                if (zipFile != null)
                {
                    try { zipFile.Close(); } catch {}
                    zipFile = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
