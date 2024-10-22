using System;
using System.Collections.Generic;

namespace XFramework.Resource
{
    [Serializable]
    public struct Manifest
    {
        public string ResourceVersion;
        public string BuildPipeline;
        public List<ManifestBundle> Bundles;
        public List<ManifestAsset> Assets;

        internal Dictionary<string, ManifestBundle> BundleForName { get; set; }

        internal Dictionary<string, ManifestAsset> AssetForPath { get; set; }

        internal Dictionary<string, ManifestAsset> AssetForAddress { get; set; }
    }
}