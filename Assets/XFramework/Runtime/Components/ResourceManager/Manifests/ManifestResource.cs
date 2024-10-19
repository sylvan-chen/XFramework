namespace XFramework
{
    public readonly struct ManifestResource
    {
        private static readonly int[] EmptyIntArray = new int[0];

        private readonly string _name;
        private readonly string _variant;
        private readonly string _extension;
        private readonly byte _loadType;
        private readonly int _size;
        private readonly int _hashCode;
        private readonly int[] _assetIndexes;

        public ManifestResource(string name, string variant, string extension, byte loadType, int size, int hashCode, int[] assetIndexes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new System.ArgumentException("Name cannot be null or empty.", nameof(name));
            }
            _name = name;
            _variant = variant;
            _extension = extension;
            _loadType = loadType;
            _size = size;
            _hashCode = hashCode;
            _assetIndexes = assetIndexes ?? EmptyIntArray;
        }

        public string Name => _name;

        public string Variant => _variant;

        public string Extension => _extension;

        public byte LoadType => _loadType;

        public int Size => _size;

        public int HashCode => _hashCode;

        public int[] AssetIndexes => _assetIndexes;
    }
}