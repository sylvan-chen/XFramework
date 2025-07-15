using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using XFramework;

[Serializable]
public class UIPanelConfig
{
    public int Id;
    public string Name;
    public string Address;
    public int ParentLayer;
}

[Serializable]
public class UIPanelConfigTable
{
    public List<UIPanelConfig> Configs = new();

    public UIPanelConfig GetConfigById(int id)
    {
        return Configs.FirstOrDefault(config => config.Id == id);
    }

    public UIPanelConfig GetConfigByAddress(string address)
    {
        return Configs.FirstOrDefault(config => config.Address == address);
    }
}
