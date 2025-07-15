using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using XFramework;

[Serializable]
public class UILayerConfig
{
    public int Id;
    public string Name;
    public int SortingOrder;
    public int StackSwitchType;
}

[Serializable]
public class UILayerConfigTable
{
    public List<UILayerConfig> Configs = new();

    public UILayerConfig GetConfigById(int id)
    {
        return Configs.FirstOrDefault(config => config.Id == id);
    }
}
