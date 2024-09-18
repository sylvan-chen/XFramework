using System;
using System.Collections.Generic;
using UnityEngine;

namespace XFramework.Unity
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/EventManager")]
    public sealed partial class EventManager : BaseManager
    {
        /// <summary>
        /// 事件字典
        /// </summary>
        /// <remarks>
        /// key 为事件 ID，value 为事件委托调用链。
        /// </remarks>
        private readonly Dictionary<int, EventHandlerChain> _eventDict = new();

        /// <summary>
        /// 延迟发布事件列表
        /// </summary>
        private readonly XLinkedList<DelayEventWrapper> _delayedEvents = new();

        private void Update()
        {
            lock (_delayedEvents)
            {
                var node = _delayedEvents.First;
                while (node != null)
                {
                    DelayEventWrapper wrapper = node.Value;
                    wrapper.DelayFrame--;
                    if (wrapper.DelayFrame <= 0)
                    {
                        wrapper.HandlerChain.Fire(wrapper.Args);
                        _delayedEvents.Remove(node);
                    }
                    node = node.Next;
                }
            }
        }

        protected override void OnShutdown()
        {
            XLog.Debug("EventManager OnShutdown");
        }

        /// <summary>
        /// 查询对应 ID 事件的委托数量
        /// </summary>
        /// <param name="id">要查询的事件 ID</param>
        public int EventHandlerCount(int id)
        {
            if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                return handlerChain.Count;
            }
            else
            {
                throw new ArgumentException($"Event (id: {id}) does not exist.");
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="id">要订阅的事件 ID</param>
        /// <param name="handler">事件委托</param>
        public void Subscribe(int id, Action<IEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler", "Handler cannot be null.");
            }
            if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChian))
            {
                handlerChian.AddHandler(handler);
            }
            else
            {
                _eventDict.Add(id, new EventHandlerChain());
                _eventDict[id].AddHandler(handler);
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="id">要取消订阅的事件 ID</param>
        /// <param name="handler">事件委托</param>
        public void Unsubscribe(int id, Action<IEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler", "Handler cannot be null.");
            }
            if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                handlerChain.RemoveHandler(handler);
                if (handlerChain.Count == 0)
                {
                    _eventDict.Remove(id);
                }
            }
            else
            {
                throw new ArgumentException($"Event (id: {id}) does not exist.");
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="id">要发布的事件 ID</param>
        /// <param name="args">事件参数</param>
        public void Publish(int id, IEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args", "EventArgs cannot be null.");
            }
            if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                handlerChain.Fire(args);
            }
            else
            {
                throw new ArgumentException($"Event (id: {id}) does not exist.");
            }
        }

        /// <summary>
        /// 延迟发布事件
        /// </summary>
        /// <param name="id">要发布的事件 ID</param>
        /// <param name="args">事件参数</param>
        /// <param name="delayFrame">延迟帧数</param>
        public void PublishLater(int id, IEventArgs args, int delayFrame = 1)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args", "EventArgs cannot be null.");
            }
            lock (_delayedEvents)
            {
                if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
                {
                    _delayedEvents.AddLast(new DelayEventWrapper(args, handlerChain, delayFrame));
                }
                else
                {
                    throw new ArgumentException($"Event (id: {id}) does not exist.");
                }
            }
        }
    }
}
