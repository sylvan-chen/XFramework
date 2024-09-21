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

        private void Awake()
        {
            GlobalManager.Register<IEventManager>(this);
        }

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

        public int EventHandlerCount(int id)
        {
            if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                return handlerChain.Count;
            }
            else
            {
                throw new ArgumentException($"[XFramework] [EventManager] Event (id: {id}) does not exist.");
            }
        }

        public void Subscribe(int id, Action<IEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler", "[XFramework] [EventManager] Subscribe(): Handler cannot be null.");
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
                throw new ArgumentNullException("handler", "[XFramework] [EventManager] Unsubscribe(): Handler cannot be null.");
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
                throw new ArgumentException($"[XFramework] [EventManager] Event (id: {id}) does not exist.");
            }
        }

        public void Publish(int id, IEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args", "[XFramework] [EventManager] EventArgs cannot be null.");
            }
            if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                handlerChain.Fire(args);
            }
            else
            {
                throw new ArgumentException($"[XFramework] [EventManager] Event (id: {id}) does not exist.");
            }
        }

        public void PublishLater(int id, IEventArgs args, int delayFrame = 1)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args", "[XFramework] [EventManager] EventArgs cannot be null.");
            }
            lock (_delayedEvents)
            {
                if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
                {
                    _delayedEvents.AddLast(new DelayEventWrapper(args, handlerChain, delayFrame));
                }
                else
                {
                    throw new ArgumentException($"[XFramework] [EventManager] Event (id: {id}) does not exist.");
                }
            }
        }

        public void RemoveAllSubscribe()
        {
            _eventDict.Clear();
        }
    }
}
