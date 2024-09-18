using System;
using System.Collections.Generic;

namespace XFramework
{
    internal sealed partial class EventModule : BaseModule, IEventModule
    {
        /// <summary>
        /// 事件字典
        /// </summary>
        /// <remarks>
        /// key 为事件 ID，value 为事件委托调用链。
        /// </remarks>
        private readonly Dictionary<int, EventHandlerChain> _events = new();

        /// <summary>
        /// 延迟发布事件列表
        /// </summary>
        private readonly XLinkedList<DelayEventWrapper> _delayedEvents = new();

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

        public void Publish(IEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args", "EventArgs cannot be null.");
            }
            if (_events.TryGetValue(args.EventId, out EventHandlerChain handlerChain))
            {
                handlerChain.Fire(args);
            }
            else
            {
                throw new ArgumentException($"Event (id: {args.EventId}) does not exist.");
            }
        }

        public void PublishLater(IEventArgs args, int delayFrame = 1)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args", "EventArgs cannot be null.");
            }
            lock (_delayedEvents)
            {
                if (_events.TryGetValue(args.EventId, out EventHandlerChain handlerChain))
                {
                    _delayedEvents.AddLast(new DelayEventWrapper(args, handlerChain, delayFrame));
                }
                else
                {
                    throw new ArgumentException($"Event (id: {args.EventId}) does not exist.");
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
            _delayedEvents.ClearEntirely();
        }

        internal override void Update(float logicSeconds, float realSeconds)
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
    }
}