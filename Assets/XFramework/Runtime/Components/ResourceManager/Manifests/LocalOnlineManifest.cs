namespace XFramework
{
    public readonly struct LocalOnlineManifest
    {
        public static readonly ManifestFileSystem[] EmptyFileSystemArray = new ManifestFileSystem[0];
        public static readonly ManifestResource[] EmptyResourceArray = new ManifestResource[0];

        private readonly bool _isValid;
        private readonly ManifestFileSystem[] _fileSystems;
        private readonly ManifestResource[] _resources;

        public LocalOnlineManifest(ManifestFileSystem[] fileSystems, ManifestResource[] resources)
        {
            _isValid = true;

            _fileSystems = fileSystems ?? EmptyFileSystemArray;
            _resources = resources ?? EmptyResourceArray;
        }

        public bool IsValid => _isValid;

        public ManifestFileSystem[] FileSystems => _isValid ? _fileSystems : throw new System.InvalidOperationException("Manifest is not valid");

        public ManifestResource[] Resources => _isValid ? _resources : throw new System.InvalidOperationException("Manifest is not valid");
    }
}