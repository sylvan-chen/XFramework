using System;
using System.Collections.Generic;
using UnityEngine;

namespace XFramework
{
    public sealed partial class EventManager : MonoBehaviour, IEventManager
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

        public int GetEventHandlerCount(int id)
        {
            if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                return handlerChain.Count;
            }
            else
            {
                throw new ArgumentException($"GetEventHandlerCount failed, event id {id} does not exist.", nameof(id));
            }
        }

        public void Subscribe(int id, Action<IEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler), "Subscribe failed, handler cannot be null.");
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

        public void Unsubscribe(int id, Action<IEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler), "Unsubscribe failed, handler cannot be null.");
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
                throw new ArgumentException($"Unsubscribe failed, event id {id} does not exist.", nameof(id));
            }
        }

        public void Publish(int id, IEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args), "Publish failed, event arguments cannot be null.");
            }
            if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                handlerChain.Fire(args);
            }
            else
            {
                throw new ArgumentException($"Publish failed, event id {id} does not exist.", nameof(id));
            }
        }

        public void PublishLater(int id, IEventArgs args, int delayFrame = 1)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args), "PublishLater failed, event arguments cannot be null.");
            }
            lock (_delayedEvents)
            {
                if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
                {
                    _delayedEvents.AddLast(new DelayEventWrapper(args, handlerChain, delayFrame));
                }
                else
                {
                    throw new ArgumentException($"PublishLater failed, event id {id} does not exist.", nameof(id));
                }
            }
        }

        public void RemoveAllSubscribe()
        {
            _eventDict.Clear();
        }
    }
}
