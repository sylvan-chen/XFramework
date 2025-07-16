using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class UILayerConfig : ConfigBase
{
    public int SortingOrder;
    public int StackSwitchType;
}

[Serializable]
public class UILayerConfigTable : ConfigTableBase<UILayerConfig>
{
    public List<UILayerConfig> Configs;

    public override UILayerConfig GetConfigById(int id)
    {
        return Configs.FirstOrDefault(config => config.Id == id);
    }
}
