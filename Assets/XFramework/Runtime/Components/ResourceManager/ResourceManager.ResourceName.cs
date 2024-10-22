namespace XFramework
{
    public sealed partial class ResourceManager
    {
        private readonly struct ResourceName
        {
            private readonly string _name;
            private readonly string _variant;
            private readonly string _extension;
            private readonly string _fullName;

            public ResourceName(string name, string variant, string extension)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new System.ArgumentException("Name cannot be null or empty", nameof(name));
                }
                if (string.IsNullOrEmpty(extension))
                {
                    throw new System.ArgumentException("Extension cannot be null or empty", nameof(extension));
                }

                _name = name;
                _variant = variant;
                _extension = extension;
                _fullName = name + (variant != null ? "." + variant : "") + "." + extension;
            }

            public string Name => _name;

            public string Variant => _variant;

            public string Extension => _extension;

            public string FullName => _fullName;

            public override string ToString()
            {
                return _fullName;
            }

            public override int GetHashCode()
            {
                if (_variant == null)
                {
                    return _name.GetHashCode() ^ _extension.GetHashCode();
                }
                else
                {
                    return _name.GetHashCode() ^ _variant.GetHashCode() ^ _extension.GetHashCode();
                }
            }

            public override bool Equals(object obj)
            {
                return obj is ResourceName ohter &&
                       string.Equals(_name, ohter._name, System.StringComparison.Ordinal) &&
                       string.Equals(_variant, ohter._variant, System.StringComparison.Ordinal) &&
                       string.Equals(_extension, ohter._extension, System.StringComparison.Ordinal);
            }

            public static bool operator ==(ResourceName left, ResourceName right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ResourceName left, ResourceName right)
            {
                return !left.Equals(right);
            }
        }
    }
}