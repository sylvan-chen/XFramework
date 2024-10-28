using System;

namespace XFramework.Resource
{
    [Serializable]
    public struct ManifestAsset
    {
        public string Hash;
        public string Address;
        public string Path;
        public string[] Tags;
        public string BundleFileName;
    }
}