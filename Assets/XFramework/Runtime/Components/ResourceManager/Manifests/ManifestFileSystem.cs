using System;

namespace XFramework
{
    public readonly struct ManifestFileSystem
    {
        private static readonly int[] EmptyIntArray = new int[0];

        private readonly string _name;
        private readonly int[] _resourceIndexes;

        public ManifestFileSystem(string name, int[] resourceIndexes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            }
            _name = name;
            _resourceIndexes = resourceIndexes ?? EmptyIntArray;
        }

        public string Name => _name;

        public int[] ResourceIndexes => _resourceIndexes;
    }
}