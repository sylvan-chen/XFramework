using System;

namespace XFramework
{
    public interface IEventManager : IManager
    {
        /// <summary>
        /// 对应 ID 事件的处理器数量
        /// </summary>
        /// <param name="id">事件 ID</param>
        public int EventHandlerCount(int id);

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="id">要订阅的事件 ID</param>
        /// <param name="handler">事件处理器</param>
        public void Subscribe(int id, EventHandler<IEventArgs> handler);

        /// <summary>
        /// 取消订阅 T 类型的事件
        /// </summary>
        /// <param name="handler">事件处理器</param>
        public void Unsubscribe(int id, EventHandler<IEventArgs> handler);

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="publisher">事件发布者</param>
        /// <param name="args">事件参数</param>
        public void Publish(object publisher, IEventArgs args);

        /// <summary>
        /// 清空所有事件
        /// </summary>
        public void ClearAll();
    }
}