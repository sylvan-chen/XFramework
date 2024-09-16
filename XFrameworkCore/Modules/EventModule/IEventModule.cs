using System;

namespace XFramework
{
    public interface IEventModule : IModule
    {
        /// <summary>
        /// 对应 ID 事件的委托数量（委托调用链长度）
        /// </summary>
        /// <param name="id">事件 ID</param>
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
        /// <param name="args">事件参数</param>
        public void Publish(IEventArgs args);

        /// <summary>
        /// 延迟发布事件
        /// </summary>
        /// <param name="args">事件参数</param>
        /// <param name="delayFrame">延迟帧数</param>
        public void PublishLater(IEventArgs args, int delayFrame = 1);
    }
}