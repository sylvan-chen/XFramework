using System;
using System.Collections.Generic;
using XFramework.Utils;

namespace XFramework
{
    public sealed partial class EventManager : ManagerBase
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
                        wrapper.Destroy();
                    }
                    node = node.Next;
                }
            }
        }

        /// <summary>
        /// 获取事件的委托数量
        /// </summary>
        /// <param name="id">要查询的事件 ID</param>
        public int GetEventHandlerCount(int id)
        {
            if (_eventDict.TryGetValue(id, out EventHandlerChain handlerChain))
            {
                return handlerChain.Count;
            }
            else
            {
                Log.Error($"GetEventHandlerCount failed, event id {id} does not exist.");
                return 0;
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
                Log.Error($"Unsubscribe failed, event id {id} does not exist.");
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
                Log.Error($"Publish failed, event id {id} does not exist.");
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
                    _delayedEvents.AddLast(DelayEventWrapper.Create(args, handlerChain, delayFrame));
                }
                else
                {
                    Log.Error($"PublishLater failed, event id {id} does not exist.");
                }
            }
        }

        /// <summary>
        /// 移除所有订阅
        /// </summary>
        public void RemoveAllSubscriptions()
        {
            _eventDict.Clear();
        }
    }
}
