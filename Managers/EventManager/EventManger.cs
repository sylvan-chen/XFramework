using System;
using System.Collections.Generic;

namespace XFramework
{
    internal sealed partial class EventManger : BaseManager, IEventManager
    {
        /// <summary>
        /// 事件字典
        /// </summary>
        /// <remarks>
        /// key 为事件 ID，value 为事件委托调用链。
        /// </remarks>
        private readonly Dictionary<int, EventHandlerChain> _events = new();

        /// <summary>
        /// 发布队列
        /// </summary>
        /// <remarks>
        /// 用于存储等待延迟发布的事件。
        /// </remarks>
        private readonly Queue<KeyValuePair<int, EventHandlerChain>> _publishQueue = new();

        internal override int Priority
        {
            get { return 0; }
        }

        public void ClearAll()
        {
            foreach (KeyValuePair<int, EventHandlerChain> pair in _events)
            {
            }
        }

        public int EventHandlerCount(int id)
        {
            throw new NotImplementedException();
        }

        public void Publish(int id, IEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args", "EventArgs cannot be null.");
            }
            if (_events.TryGetValue(id, out EventHandlerChain handlers))
            {
                handlers.Fire(args);
            }
            else
            {
                XLogger.Warning($"[XFramework] [EventManager] Event (id: {id}) has been published but there are no subscribers.");
            }
        }

        public void Subscribe(int id, Action<IEventArgs> handler)
        {
            if (_events.TryGetValue(id, out EventHandlerChain handlers))
            {
                handlers.AddHandler(handler);
            }
            else
            {
                _events[id] = new EventHandlerChain();
                _events[id].AddHandler(handler);
            }
        }

        public void Unsubscribe(int id, Action<IEventArgs> handler)
        {
            if (_events.TryGetValue(id, out EventHandlerChain handlers))
            {
                handlers.RemoveHandler(handler);
                if (handlers.Count == 0)
                {
                    _events.Remove(id);
                }
            }
            else
            {
                XLogger.Warning($"[XFramework] [EventManager] Try to unsubscribe event (id: {id}) but there are no subscribers.");
            }
        }

        internal override void Shutdown()
        {
            throw new NotImplementedException();
        }

        internal override void Update(float logicSeconds, float realSeconds)
        {
            throw new NotImplementedException();
        }
    }
}