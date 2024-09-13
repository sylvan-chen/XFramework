using System;

namespace XFramework
{
    public interface IEventManager : IManager
    {
        /// <summary>
        /// 对应 ID 事件的处理器数量（委托调用链长度）
        /// </summary>
        /// <param name="id">事件 ID</param>
        public int EventHandlerCount(int id);

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="id">要订阅的事件 ID</param>
        /// <param name="handler">事件处理器</param>
        public void Subscribe(int id, Action<IEventArgs> handler);

        /// <summary>
        /// 取消订阅 T 类型的事件
        /// </summary>
        /// <param name="id">要取消订阅的事件 ID</param>
        /// <param name="handler">事件处理器</param>
        public void Unsubscribe(int id, Action<IEventArgs> handler);

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="args">事件参数</param>
        public void Publish(int id, IEventArgs args);

        /// <summary>
        /// 清空所有事件
        /// </summary>
        public void ClearAll();
    }
}