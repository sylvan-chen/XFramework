using System;
using UnityEngine;
using XFramework;

namespace XFrameworkUnity
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/EventManager")]
    public sealed class EventManager : BaseManager
    {
        private readonly IEventSystem _eventSystem = XFrameworkGlobal.GetSystem<IEventSystem>();

        protected override void Awake()
        {
            base.Awake();
            if (_eventSystem == null)
            {
                XLog.Fatal("[XFramework] [EventManager] EventSystem is not found, please check if it is registered in XFrameworkGlobal");
                Global.Instance.Shutdown();
            }
        }

        /// <summary>
        /// 查询对应 ID 事件的委托数量
        /// </summary>
        /// <param name="id">要查询的事件 ID</param>
        public int EventHandlerCount(int id)
        {
            return _eventSystem.EventHandlerCount(id);
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="id">要订阅的事件 ID</param>
        /// <param name="handler">事件委托</param>
        public void Subscribe(int id, Action<IEventArgs> handler)
        {
            _eventSystem.Subscribe(id, handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="id">要取消订阅的事件 ID</param>
        /// <param name="handler">事件委托</param>
        public void Unsubscribe(int id, Action<IEventArgs> handler)
        {
            _eventSystem.Unsubscribe(id, handler);
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="args">事件参数</param>
        public void Publish(IEventArgs args)
        {
            _eventSystem.Publish(args);
        }

        /// <summary>
        /// 延迟发布事件
        /// </summary>
        /// <param name="args">事件参数</param>
        /// <param name="delayFrame">延迟帧数</param>
        public void PublishLater(IEventArgs args, int delayFrame = 1)
        {
            _eventSystem.PublishLater(args, delayFrame);
        }
    }
}
