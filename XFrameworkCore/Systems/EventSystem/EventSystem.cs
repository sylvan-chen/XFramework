using System;
using System.Collections.Generic;

namespace XFramework
{
    internal sealed partial class EventSystem : BaseSystem, IEventSystem
    {
        /// <summary>
        /// 事件字典
        /// </summary>
        /// <remarks>
        /// key 为事件 ID，value 为事件委托调用链。
        /// </remarks>
        private readonly Dictionary<int, EventHandlerChain> _events = new();

        /// <summary>
        /// 延迟发布队列
        /// </summary>
        private readonly Queue<DelayEventWrapper> _delayPublishQueue = new();

        internal override int Priority
        {
            get { return 0; }
        }

        public int EventHandlerCount(int id)
        {
            if (_events.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                return handlerChain.Count;
            }
            else
            {
                throw new ArgumentException($"Event (id: {id}) does not exist.");
            }
        }

        public void Publish(int id, IEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args", "EventArgs cannot be null.");
            }
            if (_events.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                handlerChain.Fire(args);
            }
            else
            {
                throw new ArgumentException($"Event (id: {id}) does not exist.");
            }
        }

        public void PublishLater(int id, IEventArgs args, int delayFrame = 1)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args", "EventArgs cannot be null.");
            }
            lock (_delayPublishQueue)
            {
                if (_events.TryGetValue(id, out EventHandlerChain handlerChain))
                {
                    // TODO: GC 优化 - 从对象池中获取 DelayEventWrapper 对象
                    _delayPublishQueue.Enqueue(new DelayEventWrapper(args, handlerChain, delayFrame));
                }
                else
                {
                    throw new ArgumentException($"Event (id: {id}) does not exist.");
                }
            }
        }

        public void Subscribe(int id, Action<IEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler", "Handler cannot be null.");
            }
            if (_events.TryGetValue(id, out EventHandlerChain handlerChian))
            {
                handlerChian.AddHandler(handler);
            }
            else
            {
                _events.Add(id, new EventHandlerChain());
                _events[id].AddHandler(handler);
            }
        }

        public void Unsubscribe(int id, Action<IEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler", "Handler cannot be null.");
            }
            if (_events.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                handlerChain.RemoveHandler(handler);
                if (handlerChain.Count == 0)
                {
                    _events.Remove(id);
                }
            }
            else
            {
                throw new ArgumentException($"Event (id: {id}) does not exist.");
            }
        }

        internal override void Shutdown()
        {
            _events.Clear();
            _delayPublishQueue.Clear();
        }

        internal override void Update(float logicSeconds, float realSeconds)
        {
            lock (_delayPublishQueue)
            {
                while (_delayPublishQueue.Count > 0)
                {
                    DelayEventWrapper eventWrapper = _delayPublishQueue.Dequeue();
                    eventWrapper.DelayFrame--;
                    if (eventWrapper.DelayFrame <= 0)
                    {
                        eventWrapper.HandlerChain.Fire(eventWrapper.Args);
                        // TODO: GC 优化 - 将 eventWrapper 回收到对象池
                    }
                }
            }
        }
    }
}