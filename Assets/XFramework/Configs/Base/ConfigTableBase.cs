using System;

[Serializable]
public abstract class ConfigTableBase
{
}

[Serializable]
public abstract class ConfigTableBase<T> : ConfigTableBase where T : ConfigBase
{
    public abstract T GetConfigById(int id);
}
