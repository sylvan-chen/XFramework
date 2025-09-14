namespace XGame.Core
{
    /// <summary>
    /// 事件接口
    /// </summary>
    public interface IEvent : ICache
    {
        public void Destroy();
    }
}