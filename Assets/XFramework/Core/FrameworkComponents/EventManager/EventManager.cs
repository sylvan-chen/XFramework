using System;
using System.Collections.Generic;
using UnityEngine;

namespace XGame.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Event Manager")]
    public sealed partial class EventManager : FrameworkComponent
    {
        /// <summary>
        /// 事件字典
        /// </summary>
        private readonly Dictionary<int, EventListenerChain> _listenerChainMap = new();

        /// <summary>
        /// 延迟发布事件列表
        /// </summary>
        private readonly XLinkedList<DelayedEvent> _delayedEvents = new();

        public int EventCount => _listenerChainMap.Count;
        public int DelayedEventCount => _delayedEvents.Count;

        internal override void Initialize()
        {
            base.Initialize();
        }

        internal override void Dispose()
        {
            base.Dispose();

            foreach (EventListenerChain listenerChain in _listenerChainMap.Values)
            {
                listenerChain.Destroy();
            }
            foreach (DelayedEvent delayedEvent in _delayedEvents)
            {
                delayedEvent.Destroy();
            }
            _listenerChainMap.Clear();
            _delayedEvents.Clear();
        }

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            base.Update(deltaTime, unscaledDeltaTime);

            lock (_delayedEvents)
            {
                var node = _delayedEvents.First;
                while (node != null)
                {
                    DelayedEvent delayedEvent = node.Value;
                    delayedEvent.DelaySeconds -= deltaTime;
                    if (delayedEvent.DelaySeconds <= 0)
                    {
                        delayedEvent.ListenerChain.Invoke(delayedEvent.Event);
                        _delayedEvents.Remove(node);
                        delayedEvent.Destroy();
                    }
                    node = node.Next;
                }
            }
        }

        public void AddListener(int id, Action<IEvent> listener)
        {
            if (listener == null)
            {
                Log.Error("[EventManager] AddListener failed, listener cannot be null.");
                return;
            }

            if (_listenerChainMap.TryGetValue(id, out EventListenerChain listenerChian))
            {
                listenerChian.AddListener(listener);
            }
            else
            {
                _listenerChainMap.Add(id, EventListenerChain.Create());
                _listenerChainMap[id].AddListener(listener);
            }
        }

        public void RemoveListener(int id, Action<IEvent> listener)
        {
            if (listener == null)
            {
                Log.Error("[EventManager] RemoveListener failed, listener cannot be null.");
                return;
            }
            if (_listenerChainMap.TryGetValue(id, out EventListenerChain listenerChain))
            {
                listenerChain.RemoveListener(listener);
                if (listenerChain.Count == 0)
                {
                    listenerChain.Destroy();
                    _listenerChainMap.Remove(id);
                }
            }
            else
            {
                Log.Error($"[EventManager] RemoveListener failed, event id {id} does not exist.");
            }
        }

        public void Dispatch(int id, IEvent evt)
        {
            if (evt == null)
            {
                Log.Error("[EventManager] Dispatch failed, event arguments cannot be null.");
                return;
            }

            if (_listenerChainMap.TryGetValue(id, out EventListenerChain listenerChain))
            {
                listenerChain.Invoke(evt);
            }
            else
            {
                Log.Error($"[EventManager] Dispatch failed, event id {id} does not exist.");
            }
            evt.Destroy();
        }

        public void DispatchLater(int id, IEvent evt, float delaySeconds = 1f)
        {
            if (evt == null)
            {
                Log.Error("[EventManager] DispatchLater failed, event arguments cannot be null.");
                return;
            }
            lock (_delayedEvents)
            {
                if (_listenerChainMap.TryGetValue(id, out EventListenerChain handlerChain))
                {
                    _delayedEvents.AddLast(DelayedEvent.Create(evt, handlerChain, delaySeconds));
                }
                else
                {
                    Log.Error($"[EventManager] DispatchLater failed, event id {id} does not exist.");
                }
            }
        }

        /// <summary>
        /// 移除所有监听
        /// </summary>
        public void ClearAllListeneers()
        {
            foreach (EventListenerChain handlerChain in _listenerChainMap.Values)
            {
                handlerChain.Destroy();
            }
            _listenerChainMap.Clear();
        }
    }
}
