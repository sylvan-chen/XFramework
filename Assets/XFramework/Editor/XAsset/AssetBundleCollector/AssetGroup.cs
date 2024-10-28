using System.Collections.Generic;

namespace XFramework.XAsset
{
    public sealed class AssetGroup
    {
        private string _name;
        private string _description;
        private PackStrategy _packStrategy;
        private List<AssetCollector> _collectors;

        public AssetGroup(string name, string description) : this(name, description, new List<AssetCollector>(), PackStrategy.PackByGroup)
        {
        }

        public AssetGroup(string name, string description, List<AssetCollector> collectors, PackStrategy packStrategy)
        {
            _name = name;
            _description = description;
            _collectors = collectors;
            _packStrategy = packStrategy;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public PackStrategy PackStrategy
        {
            get => _packStrategy;
            set => _packStrategy = value;
        }

        public List<AssetCollector> Collectors
        {
            get => _collectors;
        }

        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(_description))
                {
                    return _name;
                }
                else
                {
                    return $"{_name} ({_description})";
                }
            }
        }
    }
}