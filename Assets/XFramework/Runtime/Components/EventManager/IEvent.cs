namespace XFramework
{
    /// <summary>
    /// 事件参数
    /// </summary>
    public interface IEvent : ICache
    {
        internal void Destroy()
        {
            Global.CachePool.Unspawn(this);
        }
    }
}