namespace XFramework
{
    /// <summary>
    /// 配置类接口，所有配置类都应实现此接口以确保具有唯一标识符。
    /// </summary>
    public interface IConfig
    {
        int Id { get; set; }
    }
}
