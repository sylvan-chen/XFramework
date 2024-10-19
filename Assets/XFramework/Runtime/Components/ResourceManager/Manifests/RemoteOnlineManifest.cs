using System;

namespace XFramework
{
    public readonly struct RemoteOnlineManifest
    {
        private static readonly ManifestAsset[] EmptyAssetArray = new ManifestAsset[0];
        private static readonly ManifestFileSystem[] EmptyFileSystemArray = new ManifestFileSystem[0];
        private static readonly ManifestResource[] EmptyResourceArray = new ManifestResource[0];
        private static readonly ManifeestResourceGroup[] EmptyResourceGroupArray = new ManifeestResourceGroup[0];

        private readonly bool _isValid;
        private readonly string _resourceVersion;
        private readonly ManifestAsset[] _assets;
        private readonly ManifestFileSystem[] _fileSystems;
        private readonly ManifestResource[] _resources;
        private readonly ManifeestResourceGroup[] _resourceGroups;

        public RemoteOnlineManifest(string resourceVersion, ManifestAsset[] assets, ManifestFileSystem[] fileSystems, ManifestResource[] resources, ManifeestResourceGroup[] resourceGroups)
        {
            _isValid = true;

            _resourceVersion = resourceVersion;
            _assets = assets ?? EmptyAssetArray;
            _fileSystems = fileSystems ?? EmptyFileSystemArray;
            _resources = resources ?? EmptyResourceArray;
            _resourceGroups = resourceGroups ?? EmptyResourceGroupArray;
        }

        public bool IsValid => _isValid;

        public string ResourceVersion => _isValid ? _resourceVersion : throw new InvalidOperationException("Manifest is not valid.");

        public ManifestAsset[] Assets => _isValid ? _assets : throw new InvalidOperationException("Manifest is not valid.");

        public ManifestFileSystem[] FileSystems => _isValid ? _fileSystems : throw new InvalidOperationException("Manifest is not valid.");

        public ManifestResource[] Resources => _isValid ? _resources : throw new InvalidOperationException("Manifest is not valid.");

        public ManifeestResourceGroup[] ResourceGroups => _isValid ? _resourceGroups : throw new InvalidOperationException("Manifest is not valid.");
    }
}