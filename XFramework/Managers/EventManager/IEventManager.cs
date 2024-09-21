using System;

namespace XFramework
{
    public interface IEventManager : IManager
    {
        /// <summary>
        /// 查询对应 ID 事件的委托数量
        /// </summary>
        /// <param name="id">要查询的事件 ID</param>
        public int EventHandlerCount(int id);

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="id">要订阅的事件 ID</param>
        /// <param name="handler">事件委托</param>
        public void Subscribe(int id, Action<IEventArgs> handler);

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="id">要取消订阅的事件 ID</param>
        /// <param name="handler">事件委托</param>
        public void Unsubscribe(int id, Action<IEventArgs> handler);

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="id">要发布的事件 ID</param>
        /// <param name="args">事件参数</param>
        public void Publish(int id, IEventArgs args);

        /// <summary>
        /// 延迟发布事件
        /// </summary>
        /// <param name="id">要发布的事件 ID</param>
        /// <param name="args">事件参数</param>
        /// <param name="delayFrame">延迟帧数</param>
        public void PublishLater(int id, IEventArgs args, int delayFrame = 1);

        /// <summary>
        /// 移除所有订阅
        /// </summary>
        public void RemoveAllSubscribe();
    }
}