using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class UIPanelConfig : ConfigBase
{
    public string Address;
    public int ParentLayer;
}

[Serializable]
public class UIPanelConfigTable : ConfigTableBase<UIPanelConfig>
{
    public List<UIPanelConfig> Configs = new();

    public override UIPanelConfig GetConfigById(int id)
    {
        return Configs.FirstOrDefault(config => config.Id == id);
    }

    public UIPanelConfig GetConfigByAddress(string address)
    {
        return Configs.FirstOrDefault(config => config.Address == address);
    }
}
