namespace XGame.Core
{
    /// <summary>
    /// 事件接口
    /// </summary>
    public interface IEvent : ICache
    {
        internal void Destroy();
    }
}