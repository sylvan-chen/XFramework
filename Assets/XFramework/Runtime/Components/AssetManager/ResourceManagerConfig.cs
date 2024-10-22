namespace XFramework.Resource
{
    public static class ResourceManagerConfig
    {
        public const string ManifestBinaryFileName = "Manifest.bytes";
        public const string ManifestJsonFileName = "Manifest.json";
        public const string ManifestHashFileName = "ManifestHash";
        public const string ResourceVersionFileName = "ResourceVersion";
        public const string ReportFileName = "BuildReport.json";
        public const string BundleExtension = ".bundle";
        public const int ManifestFileMaxSize = 1024 * 1024 * 100; // 100MB

        /// <summary>
        /// 清单的头部标记
        /// </summary>
        public static readonly byte[] ManifestBinaryFileHeaderSign = new byte[3] { (byte)'X', (byte)'F', (byte)'M' };
    }
}