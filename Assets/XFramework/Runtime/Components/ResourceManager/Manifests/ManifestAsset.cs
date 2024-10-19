using System;

namespace XFramework
{
    public readonly struct ManifestAsset
    {
        private static readonly int[] EmptyIntArray = new int[0];

        private readonly string _name;
        private readonly int[] _dependenyAssetIndexes;

        public ManifestAsset(string name, int[] dependenyAssetIndexes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            }
            _name = name;
            _dependenyAssetIndexes = dependenyAssetIndexes ?? EmptyIntArray;
        }

        public string Name => _name;

        public int[] DependenyAssetIndexes => _dependenyAssetIndexes;
    }
}