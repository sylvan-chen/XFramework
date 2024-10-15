using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 事件参数
    /// </summary>
    public interface IEvent : ICache
    {
        internal void Destroy()
        {
            CachePool.Unspawn(this);
        }
    }
}