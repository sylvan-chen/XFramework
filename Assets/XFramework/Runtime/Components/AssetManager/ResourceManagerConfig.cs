using XFramework.Utils;

namespace XFramework.Resource
{
    public static class ResourceManagerSettings
    {
        public const string ResourcePackFolderName = "ResourcePack";
        public const string ManifestBinaryFileName = "ResourceManifest.bytes";
        public const string ManifestJsonFileName = "ResourceManifest.json";
        public const string ManifestHashFileName = "ResourceManifest.hash";
        public const string ResourceVersionFileName = "ResourceManifest.version";
        public const string ReportFileName = "ResourceManifest_BuildReport.json";
        public const string BundleExtension = ".bundle";
        public const int ManifestFileMaxSize = 1024 * 1024 * 100; // 100MB

        /// <summary>
        /// 清单文件的头部标记
        /// </summary>
        public static readonly byte[] ManifestBinaryFileHeaderSign = new byte[3] { (byte)'X', (byte)'F', (byte)'M' };

        public static string GetManifestBinaryFileName(string resourceVersion)
        {
            return $"Manifest_{resourceVersion}.bytes";
        }

        public static string GetManifestJsonFileName(string resourceVersion)
        {
            return $"Manifest_{resourceVersion}.json";
        }

        public static string GetManifestHashFileName(string resourceVersion)
        {
            return $"Manifest_{resourceVersion}.hash";
        }

        public static string GetResourceVersionFileName()
        {
            return $"Manifest.version";
        }

        public static string GetBuildReportFileName(string resourceVersion)
        {
            return $"Manifest_{resourceVersion}_BuildReport.json";
        }

    }
}