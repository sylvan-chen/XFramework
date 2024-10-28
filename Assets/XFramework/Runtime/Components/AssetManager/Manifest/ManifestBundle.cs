using System;

namespace XFramework.Resource
{
    [Serializable]
    public struct ManifestBundle
    {
        public string Hash;
        public string Name;
        public long FileSize;
        public bool IsEncrypted;
        public string[] Tags;
        public string[] DependenyNames;

        public readonly string FileName
        {
            get => $"{Name}{ResourceManagerSettings.BundleExtension}";
        }
    }
}