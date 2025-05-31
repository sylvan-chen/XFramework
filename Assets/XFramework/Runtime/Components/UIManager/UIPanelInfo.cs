public class UIPanelInfo
{
    private string _name;
    private string _assetName;

    public string Name
    {
        get => _name;
    }

    public string AssetName
    {
        get => _assetName;
    }

    public UIPanelInfo(string name, string assetName)
    {
        _name = name;
        _assetName = assetName;
    }
}
