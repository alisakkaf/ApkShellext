using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ApkShellext {
    public class CachedPackageInfo {
        public string AppName { get; set; }
        public string PackageName { get; set; }
        public string Version { get; set; }
        public string Revision { get; set; }
        public string Publisher { get; set; }
        public AppPackageReader.AppType Type { get; set; }
        public Bitmap Icon { get; set; }

        public Bitmap GetIconClone(Size size) {
            if (Icon == null) return null;
            lock (Icon) {
                try {
                    return Utility.ResizeBitmap(Icon, size);
                } catch {
                    return null;
                }
            }
        }
    }

    public static class PackageCache {
        private const int MaxCacheSize = 100;
        private static readonly object lockObj = new object();
        private static readonly Dictionary<string, LinkedListNode<CacheItem>> cacheMap = new Dictionary<string, LinkedListNode<CacheItem>>(StringComparer.OrdinalIgnoreCase);
        private static readonly LinkedList<CacheItem> lruList = new LinkedList<CacheItem>();

        private class CacheItem {
            public string FilePath { get; set; }
            public DateTime LastWriteTimeUtc { get; set; }
            public long FileLength { get; set; }
            public CachedPackageInfo Info { get; set; }
        }

        public static CachedPackageInfo Get(string filePath) {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            FileInfo fi = new FileInfo(filePath);
            DateTime lastWrite = fi.LastWriteTimeUtc;
            long length = fi.Length;

            lock (lockObj) {
                if (cacheMap.TryGetValue(filePath, out var node)) {
                    // Validate cache freshness
                    if (node.Value.LastWriteTimeUtc == lastWrite && node.Value.FileLength == length) {
                        // Move to front (most recently used)
                        lruList.Remove(node);
                        lruList.AddFirst(node);
                        return node.Value.Info;
                    } else {
                        // Expired entry
                        RemoveNode(node);
                    }
                }
            }

            return null;
        }

        public static void Put(string filePath, AppPackageReader reader) {
            if (string.IsNullOrEmpty(filePath) || reader == null || !File.Exists(filePath)) return;

            FileInfo fi = new FileInfo(filePath);

            CachedPackageInfo info = new CachedPackageInfo {
                AppName = reader.AppName,
                PackageName = reader.PackageName,
                Version = reader.Version,
                Revision = reader.Revision,
                Publisher = reader.Publisher,
                Type = reader.Type,
                Icon = reader.Icon != null ? (Bitmap)reader.Icon.Clone() : null
            };

            CacheItem item = new CacheItem {
                FilePath = filePath,
                LastWriteTimeUtc = fi.LastWriteTimeUtc,
                FileLength = fi.Length,
                Info = info
            };

            lock (lockObj) {
                if (cacheMap.TryGetValue(filePath, out var existingNode)) {
                    RemoveNode(existingNode);
                }

                while (cacheMap.Count >= MaxCacheSize && lruList.Last != null) {
                    RemoveNode(lruList.Last);
                }

                LinkedListNode<CacheItem> newNode = lruList.AddFirst(item);
                cacheMap[filePath] = newNode;
            }
        }

        private static void RemoveNode(LinkedListNode<CacheItem> node) {
            if (node == null) return;
            lruList.Remove(node);
            cacheMap.Remove(node.Value.FilePath);

            if (node.Value.Info != null && node.Value.Info.Icon != null) {
                try {
                    node.Value.Info.Icon.Dispose();
                } catch { }
                node.Value.Info.Icon = null;
            }
        }

        public static void Clear() {
            lock (lockObj) {
                foreach (var node in cacheMap.Values) {
                    if (node.Value.Info != null && node.Value.Info.Icon != null) {
                        try { node.Value.Info.Icon.Dispose(); } catch { }
                    }
                }
                cacheMap.Clear();
                lruList.Clear();
            }
        }
    }
}
